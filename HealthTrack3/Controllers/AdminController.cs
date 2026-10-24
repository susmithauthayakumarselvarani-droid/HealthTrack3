using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HealthTrack3.Helpers;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HealthTrack3.Controllers
{
    [RoleAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ---------- Dashboard (Task 4) ----------
        public ActionResult Index()
        {
            var rows = BuildAllRows();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = rows.Count,
                Clients = rows.Count(r => r.Role == "Client"),
                Trainers = rows.Count(r => r.Role == "Trainer"),
                NewUsersLast7Days = rows.Count(r => r.CreatedAt >= DateTime.Now.AddDays(-7)),
                UnassignedClients = rows.Count(r => r.Role == "Client" && string.IsNullOrEmpty(r.TrainerId)),

                PendingAppointments = db.Appointments.Count(a => a.Status == AppointmentStatus.Pending),
                ApprovedAppointments = db.Appointments.Count(a => a.Status == AppointmentStatus.Approved),
                CancelledAppointments = db.Appointments.Count(a => a.Status == AppointmentStatus.Cancelled),

                WorkoutLogs = db.WorkoutLogs.Count(),
                MealLogs = db.MealLogs.Count(),
                Measurements = db.HealthMetrics.Count(),

                RecentUsers = rows.OrderByDescending(r => r.CreatedAt).Take(8).ToList()
            };
            return View(model);
        }

        // ---------- Users (Task 4) ----------
        public ActionResult Users(string role, string q)
        {
            var all = BuildAllRows();
            var rows = all;

            if (!string.IsNullOrEmpty(role) && role != "All")
            {
                rows = rows.Where(r => r.Role == role).ToList();
            }
            if (!string.IsNullOrWhiteSpace(q))
            {
                string term = q.Trim().ToLower();
                rows = rows.Where(r => (r.FullName ?? "").ToLower().Contains(term)
                                    || (r.Email ?? "").ToLower().Contains(term)).ToList();
            }

            var model = new AdminUsersViewModel
            {
                Users = rows,
                RoleFilter = string.IsNullOrEmpty(role) ? "All" : role,
                Search = q,
                Trainers = all.Where(r => r.Role == "Trainer" && !r.IsLocked)
                    .Select(r => new SelectListItem { Value = r.Id, Text = r.FullName })
                    .ToList()
            };
            return View(model);
        }

        // Assign (or remove) the trainer of a client
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignTrainer(string clientId, string trainerId)
        {
            var all = BuildAllRows();
            var client = all.FirstOrDefault(r => r.Id == clientId && r.Role == "Client");
            if (client == null)
            {
                TempData["Error"] = "Client not found.";
                return RedirectToAction("Users");
            }

            AdminUserRow trainer = null;
            if (!string.IsNullOrEmpty(trainerId))
            {
                trainer = all.FirstOrDefault(r => r.Id == trainerId && r.Role == "Trainer");
                if (trainer == null)
                {
                    TempData["Error"] = "Please choose a valid trainer.";
                    return RedirectToAction("Users");
                }
            }

            try
            {
                var user = db.Users.FirstOrDefault(u => u.Id == clientId);
                user.TrainerId = trainer != null ? trainer.Id : null;

                if (trainer != null)
                {
                    db.Notifications.Add(new Notification
                    {
                        UserId = client.Id,
                        Type = "Assignment",
                        Message = "You have been assigned to trainer " + trainer.FullName + "."
                    });
                    db.Notifications.Add(new Notification
                    {
                        UserId = trainer.Id,
                        Type = "Assignment",
                        Message = "New client assigned to you: " + client.FullName + "."
                    });
                }
                db.SaveChanges();

                TempData["Success"] = trainer != null
                    ? client.FullName + " is now assigned to " + trainer.FullName + "."
                    : "Trainer removed from " + client.FullName + ".";
            }
            catch (Exception)
            {
                TempData["Error"] = "Could not update the assignment. Please try again.";
            }
            return RedirectToAction("Users");
        }

        // Disable / enable an account (Admin accounts cannot be disabled)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleLock(string id, string returnTo)
        {
            var target = BuildAllRows().FirstOrDefault(r => r.Id == id);
            if (target == null || target.Role == "Admin" || id == User.Identity.GetUserId())
            {
                TempData["Error"] = "This account cannot be changed.";
                return RedirectToAction(returnTo == "Trainers" ? "Trainers" : "Users");
            }

            try
            {
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                if (target.IsLocked)
                {
                    userManager.SetLockoutEndDate(id, DateTimeOffset.UtcNow.AddMinutes(-1));
                    TempData["Success"] = target.FullName + " was enabled.";
                }
                else
                {
                    userManager.SetLockoutEnabled(id, true);
                    userManager.SetLockoutEndDate(id, DateTimeOffset.UtcNow.AddYears(100));
                    TempData["Success"] = target.FullName + " was disabled and can no longer log in.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Could not change the account status. Please try again.";
            }
            return RedirectToAction(returnTo == "Trainers" ? "Trainers" : "Users");
        }

        // ---------- Trainers (Task 4) ----------
        public ActionResult Trainers()
        {
            var all = BuildAllRows();
            var list = new List<AdminTrainerRow>();

            foreach (var t in all.Where(r => r.Role == "Trainer"))
            {
                string tid = t.Id;
                list.Add(new AdminTrainerRow
                {
                    Id = t.Id,
                    FullName = t.FullName,
                    Email = t.Email,
                    IsLocked = t.IsLocked,
                    ClientCount = all.Count(r => r.TrainerId == tid),
                    PendingAppointments = db.Appointments.Count(a => a.TrainerId == tid && a.Status == AppointmentStatus.Pending),
                    TotalAppointments = db.Appointments.Count(a => a.TrainerId == tid)
                });
            }
            return View(list);
        }

        // ---------- Helpers ----------
        private List<AdminUserRow> BuildAllRows()
        {
            var users = db.Users.Include(u => u.Roles).ToList();
            var roleNames = db.Roles.ToDictionary(r => r.Id, r => r.Name);
            var names = users.ToDictionary(u => u.Id, u => DisplayName(u));

            return users.Select(u =>
            {
                string roleId = u.Roles.Select(r => r.RoleId).FirstOrDefault();
                string role = roleId != null && roleNames.ContainsKey(roleId) ? roleNames[roleId] : "None";

                string trainerName = null;
                if (!string.IsNullOrEmpty(u.TrainerId) && names.ContainsKey(u.TrainerId))
                {
                    trainerName = names[u.TrainerId];
                }

                return new AdminUserRow
                {
                    Id = u.Id,
                    FullName = DisplayName(u),
                    Email = u.Email,
                    Role = role,
                    TrainerId = u.TrainerId,
                    TrainerName = trainerName,
                    CreatedAt = u.CreatedAt,
                    IsLocked = u.LockoutEnabled && u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > DateTime.UtcNow
                };
            }).OrderBy(r => r.FullName).ToList();
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