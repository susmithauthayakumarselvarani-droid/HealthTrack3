using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HealthTrack3.Helpers;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;

namespace HealthTrack3.Controllers
{
    [RoleAuthorize("Client")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ---------- Dashboard (Task 2) ----------
        public ActionResult Index()
        {
            string clientId = User.Identity.GetUserId();
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            DateTime now = DateTime.Now;
            var user = db.Users.FirstOrDefault(u => u.Id == clientId);

            var model = new ClientDashboardViewModel
            {
                FullName = user != null && !string.IsNullOrEmpty(user.FullName) ? user.FullName : User.Identity.Name,

                LatestMetric = db.HealthMetrics
                    .Where(m => m.ClientId == clientId)
                    .OrderByDescending(m => m.RecordedOn)
                    .ThenByDescending(m => m.Id)
                    .FirstOrDefault(),

                WorkoutPlans = db.WorkoutPlans.Include(p => p.Trainer)
                    .Where(p => p.ClientId == clientId && p.EndDate >= today)
                    .OrderBy(p => p.StartDate)
                    .ToList(),

                DietPlans = db.DietPlans.Include(p => p.Trainer)
                    .Where(p => p.ClientId == clientId && p.EndDate >= today)
                    .OrderBy(p => p.StartDate)
                    .ToList(),

                UpcomingAppointments = db.Appointments.Include(a => a.Trainer)
                    .Where(a => a.ClientId == clientId
                                && a.AppointmentDate >= now
                                && a.Status != AppointmentStatus.Cancelled)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(5)
                    .ToList(),

                TodayCaloriesBurned = db.WorkoutLogs
                    .Where(w => w.ClientId == clientId && w.LoggedOn >= today && w.LoggedOn < tomorrow)
                    .Select(w => (int?)w.CaloriesBurned).Sum() ?? 0,

                TodayCaloriesEaten = db.MealLogs
                    .Where(m => m.ClientId == clientId && m.LoggedOn >= today && m.LoggedOn < tomorrow)
                    .Select(m => (int?)m.Calories).Sum() ?? 0
            };

            return View(model);
        }

        // ---------- Health measurements (Task 5) ----------
        public ActionResult Metrics()
        {
            ViewBag.History = GetMetricHistory();
            return View(new HealthMetric());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Metrics([Bind(Include = "RecordedOn,WeightKg,HeightCm,CaloriesBurned")] HealthMetric metric)
        {
            // The client id comes from the login, never from the form
            metric.ClientId = User.Identity.GetUserId();
            ModelState.Remove("ClientId");
            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                try
                {
                    double heightInMetres = metric.HeightCm / 100.0;
                    metric.Bmi = Math.Round(metric.WeightKg / (heightInMetres * heightInMetres), 1);

                    db.HealthMetrics.Add(metric);
                    db.SaveChanges();

                    TempData["Success"] = "Health measurement saved.";
                    return RedirectToAction("Metrics");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Could not save the measurement. Please try again.");
                }
            }

            ViewBag.History = GetMetricHistory();
            return View(metric);
        }

        // ---------- Workout log (Task 5) ----------
        public ActionResult Workouts()
        {
            ViewBag.History = GetWorkoutHistory();
            return View(new WorkoutLog());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Workouts([Bind(Include = "LoggedOn,ExerciseName,DurationMinutes,CaloriesBurned")] WorkoutLog log)
        {
            log.ClientId = User.Identity.GetUserId();
            ModelState.Remove("ClientId");
            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                try
                {
                    db.WorkoutLogs.Add(log);
                    db.SaveChanges();

                    TempData["Success"] = "Workout logged.";
                    return RedirectToAction("Workouts");
                }
                catch (Exception ex)
                {
                    // TEMPORARY: shows the real reason while we debug. Remove before submission.
                    ModelState.AddModelError("", "Could not save the workout: " + ex.GetBaseException().Message);
                }
            }

            ViewBag.History = GetWorkoutHistory();
            return View(log);
        }

        // ---------- Meal log (Task 5) ----------
        public ActionResult Meals()
        {
            ViewBag.MealTypes = GetMealTypes();
            ViewBag.History = GetMealHistory();
            return View(new MealLog());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Meals([Bind(Include = "LoggedOn,MealType,FoodName,Calories")] MealLog meal)
        {
            meal.ClientId = User.Identity.GetUserId();
            ModelState.Remove("ClientId");
            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                try
                {
                    db.MealLogs.Add(meal);
                    db.SaveChanges();

                    TempData["Success"] = "Meal logged.";
                    return RedirectToAction("Meals");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Could not save the meal. Please try again.");
                }
            }

            ViewBag.MealTypes = GetMealTypes();
            ViewBag.History = GetMealHistory();
            return View(meal);
        }

        // ---------- Helpers ----------
        private List<HealthMetric> GetMetricHistory()
        {
            string clientId = User.Identity.GetUserId();
            return db.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.RecordedOn)
                .ThenByDescending(m => m.Id)
                .Take(15)
                .ToList();
        }

        private List<WorkoutLog> GetWorkoutHistory()
        {
            string clientId = User.Identity.GetUserId();
            return db.WorkoutLogs
                .Where(w => w.ClientId == clientId)
                .OrderByDescending(w => w.LoggedOn)
                .ThenByDescending(w => w.Id)
                .Take(15)
                .ToList();
        }

        private List<MealLog> GetMealHistory()
        {
            string clientId = User.Identity.GetUserId();
            return db.MealLogs
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.LoggedOn)
                .ThenByDescending(m => m.Id)
                .Take(15)
                .ToList();
        }

        private SelectList GetMealTypes()
        {
            return new SelectList(new[] { "Breakfast", "Lunch", "Dinner", "Snack" });
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