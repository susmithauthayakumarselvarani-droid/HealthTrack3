using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HealthTrack3.Helpers;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;

namespace HealthTrack3.Controllers
{
    [RoleAuthorize("Trainer")]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ---------- Dashboard (Task 3) ----------
        public ActionResult Index()
        {
            string trainerId = User.Identity.GetUserId();
            var trainer = db.Users.FirstOrDefault(u => u.Id == trainerId);
            var clients = db.Users.Where(u => u.TrainerId == trainerId).OrderBy(u => u.FullName).ToList();

            var rows = new List<TrainerClientRow>();
            foreach (var c in clients)
            {
                var latest = db.HealthMetrics.Where(m => m.ClientId == c.Id)
                    .OrderByDescending(m => m.RecordedOn).ThenByDescending(m => m.Id).FirstOrDefault();
                var lastWorkout = db.WorkoutLogs.Where(w => w.ClientId == c.Id)
                    .OrderByDescending(w => w.LoggedOn).FirstOrDefault();

                rows.Add(new TrainerClientRow
                {
                    Id = c.Id,
                    FullName = string.IsNullOrEmpty(c.FullName) ? c.Email : c.FullName,
                    Email = c.Email,
                    LatestWeight = latest != null ? (double?)latest.WeightKg : null,
                    LatestBmi = latest != null ? (double?)latest.Bmi : null,
                    LastWorkout = lastWorkout != null ? (DateTime?)lastWorkout.LoggedOn : null
                });
            }

            var model = new TrainerDashboardViewModel
            {
                FullName = trainer != null && !string.IsNullOrEmpty(trainer.FullName) ? trainer.FullName : User.Identity.Name,
                Clients = rows,
                PendingAppointments = db.Appointments.Count(a => a.TrainerId == trainerId && a.Status == AppointmentStatus.Pending)
            };
            return View(model);
        }

        // ---------- Client progress (Task 3) ----------
        public ActionResult Progress(string id)
        {
            var client = GetOwnClient(id);
            if (client == null) return Redirect("~/Home/AccessDenied");

            string trainerId = User.Identity.GetUserId();
            var model = new ClientProgressViewModel
            {
                Client = client,
                Metrics = db.HealthMetrics.Where(m => m.ClientId == id)
                    .OrderByDescending(m => m.RecordedOn).ThenByDescending(m => m.Id).Take(10).ToList(),
                Workouts = db.WorkoutLogs.Where(w => w.ClientId == id)
                    .OrderByDescending(w => w.LoggedOn).ThenByDescending(w => w.Id).Take(10).ToList(),
                Meals = db.MealLogs.Where(m => m.ClientId == id)
                    .OrderByDescending(m => m.LoggedOn).ThenByDescending(m => m.Id).Take(10).ToList(),
                WorkoutPlans = db.WorkoutPlans.Where(p => p.ClientId == id && p.TrainerId == trainerId)
                    .OrderByDescending(p => p.StartDate).ToList(),
                DietPlans = db.DietPlans.Where(p => p.ClientId == id && p.TrainerId == trainerId)
                    .OrderByDescending(p => p.StartDate).ToList(),
                Feedbacks = db.Feedbacks.Where(f => f.ClientId == id && f.TrainerId == trainerId)
                    .OrderByDescending(f => f.CreatedAt).Take(10).ToList()
            };
            return View(model);
        }

        // ---------- Workout plan (Task 5) ----------
        public ActionResult CreateWorkoutPlan(string id)
        {
            var client = GetOwnClient(id);
            if (client == null) return Redirect("~/Home/AccessDenied");

            ViewBag.ClientName = DisplayName(client);
            return View(new WorkoutPlan { ClientId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateWorkoutPlan([Bind(Include = "ClientId,Title,Exercises,StartDate,EndDate")] WorkoutPlan plan)
        {
            var client = GetOwnClient(plan.ClientId);
            if (client == null) return Redirect("~/Home/AccessDenied");

            // The trainer id comes from the login, never from the form
            plan.TrainerId = User.Identity.GetUserId();
            ModelState.Remove("TrainerId");
            ModelState.Remove("Trainer");
            ModelState.Remove("Client");

            if (plan.EndDate < plan.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after the start date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.WorkoutPlans.Add(plan);
                    db.Notifications.Add(new Notification
                    {
                        UserId = client.Id,
                        Type = "Plan",
                        Message = "Your trainer added a new workout plan: " + plan.Title
                    });
                    db.SaveChanges();

                    TempData["Success"] = "Workout plan created.";
                    return RedirectToAction("Progress", new { id = plan.ClientId });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Could not save the plan. Please try again.");
                }
            }

            ViewBag.ClientName = DisplayName(client);
            return View(plan);
        }

        // ---------- Diet plan (Task 5) ----------
        public ActionResult CreateDietPlan(string id)
        {
            var client = GetOwnClient(id);
            if (client == null) return Redirect("~/Home/AccessDenied");

            ViewBag.ClientName = DisplayName(client);
            return View(new DietPlan { ClientId = id, DailyCalorieTarget = 2000 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDietPlan([Bind(Include = "ClientId,Title,Meals,DailyCalorieTarget,StartDate,EndDate")] DietPlan plan)
        {
            var client = GetOwnClient(plan.ClientId);
            if (client == null) return Redirect("~/Home/AccessDenied");

            plan.TrainerId = User.Identity.GetUserId();
            ModelState.Remove("TrainerId");
            ModelState.Remove("Trainer");
            ModelState.Remove("Client");

            if (plan.EndDate < plan.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be on or after the start date.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.DietPlans.Add(plan);
                    db.Notifications.Add(new Notification
                    {
                        UserId = client.Id,
                        Type = "Plan",
                        Message = "Your trainer added a new diet plan: " + plan.Title
                    });
                    db.SaveChanges();

                    TempData["Success"] = "Diet plan created.";
                    return RedirectToAction("Progress", new { id = plan.ClientId });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Could not save the plan. Please try again.");
                }
            }

            ViewBag.ClientName = DisplayName(client);
            return View(plan);
        }

        // ---------- Feedback (Task 3) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddFeedback(string clientId, string message)
        {
            var client = GetOwnClient(clientId);
            if (client == null) return Redirect("~/Home/AccessDenied");

            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                TempData["Error"] = "Feedback must be between 1 and 500 characters.";
                return RedirectToAction("Progress", new { id = clientId });
            }

            try
            {
                db.Feedbacks.Add(new Feedback
                {
                    TrainerId = User.Identity.GetUserId(),
                    ClientId = clientId,
                    Message = message.Trim()
                });
                db.Notifications.Add(new Notification
                {
                    UserId = clientId,
                    Type = "Feedback",
                    Message = "You have new feedback from your trainer."
                });
                db.SaveChanges();
                TempData["Success"] = "Feedback sent.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Could not send feedback. Please try again.";
            }
            return RedirectToAction("Progress", new { id = clientId });
        }

        // ---------- Helpers ----------
        // Returns the client only if he/she is assigned to the logged-in trainer
        private ApplicationUser GetOwnClient(string clientId)
        {
            if (string.IsNullOrEmpty(clientId)) return null;
            string trainerId = User.Identity.GetUserId();
            return db.Users.FirstOrDefault(u => u.Id == clientId && u.TrainerId == trainerId);
        }

        private static string DisplayName(ApplicationUser user)
        {
            return string.IsNullOrEmpty(user.FullName) ? user.Email : user.FullName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}