using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using HealthTrack3.Helpers;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;

namespace HealthTrack3.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // Only weekly (7) or monthly (30) are allowed
        private static int NormalizeDays(int days)
        {
            return days == 30 ? 30 : 7;
        }

        // ---------- Client: own charts ----------
        [RoleAuthorize("Client")]
        public ActionResult My(int days = 7)
        {
            days = NormalizeDays(days);
            string clientId = User.Identity.GetUserId();
            var user = db.Users.FirstOrDefault(u => u.Id == clientId);

            var data = AnalyticsService.BuildProgress(
    db,
    clientId,
    NameOf(user));
            ViewBag.Heading = "My Progress";
            ViewBag.PageAction = "My";
            ViewBag.RouteId = null;
            ViewBag.BackUrl = Url.Action("Index", "Client");
            ViewBag.BackText = "Back to dashboard";
            return View("Progress", data);
        }

        // ---------- Trainer (own clients only) / Admin (any client): charts ----------
        [RoleAuthorize("Trainer", "Admin")]
        public ActionResult ClientProgress(string id, int days = 7)
        {
            days = NormalizeDays(days);
            bool isAdmin = User.IsInRole("Admin");
            string userId = User.Identity.GetUserId();

            var client = string.IsNullOrEmpty(id)
                ? null
                : db.Users.FirstOrDefault(u => u.Id == id && (isAdmin || u.TrainerId == userId));
            if (client == null) return Redirect("~/Home/AccessDenied");

            var data = AnalyticsService.BuildProgress(
    db,
    client.Id,
    NameOf(client));
            ViewBag.Heading = NameOf(client) + "'s Progress";
            ViewBag.PageAction = "ClientProgress";
            ViewBag.RouteId = client.Id;
            ViewBag.BackUrl = Url.Action("Report", "Analytics");
            ViewBag.BackText = "Back to report";
            return View("Progress", data);
        }

        // ---------- Trainer / Admin: client report ----------
        [RoleAuthorize("Trainer", "Admin")]
        public ActionResult Report(int days = 30)
        {
            return View(BuildReport(NormalizeDays(days)));
        }

        [RoleAuthorize("Trainer", "Admin")]
        public ActionResult ReportCsv(int days = 30)
        {
            var report = BuildReport(NormalizeDays(days));
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Client,Email,Trainer,Weight (kg),BMI,BMI category,Weight change (kg),Workouts,Calories burned,Calories eaten,Last workout");

            foreach (var r in report.Rows)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(r.ClientName),
                    Csv(r.Email),
                    Csv(r.TrainerName),
                    r.Weight.HasValue ? r.Weight.Value.ToString("0.#", inv) : "",
                    r.Bmi.HasValue ? r.Bmi.Value.ToString("0.0", inv) : "",
                    Csv(r.BmiCategory),
                    r.WeightChange.HasValue ? r.WeightChange.Value.ToString("0.#", inv) : "",
                    r.Workouts.ToString(),
                    r.Burned.ToString(),
                    r.Eaten.ToString(),
                    r.LastWorkout.HasValue ? r.LastWorkout.Value.ToString("yyyy-MM-dd") : ""
                }));
            }

            // BOM first, so Excel reads the file as UTF-8
            byte[] bytes = new UTF8Encoding(true).GetPreamble()
                .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
                .ToArray();
            return File(bytes, "text/csv", "HealthTrack-Report-" + DateTime.Today.ToString("yyyyMMdd") + ".csv");
        }

        // ---------- Trainer / Admin: appointment report ----------
        [RoleAuthorize("Trainer", "Admin")]
        public ActionResult AppointmentReport(int days = 30)
        {
            days = NormalizeDays(days);
            bool isAdmin = User.IsInRole("Admin");
            string userId = User.Identity.GetUserId();
            DateTime since = DateTime.Today.AddDays(-(days - 1));

            var query = db.Appointments.Include(a => a.Client).Include(a => a.Trainer)
                .Where(a => a.AppointmentDate >= since);
            if (!isAdmin)
            {
                query = query.Where(a => a.TrainerId == userId);
            }

            var rows = query.OrderBy(a => a.AppointmentDate).Take(500).ToList()
                .Select(a => new AppointmentReportRow
                {
                    When = a.AppointmentDate,
                    ClientName = NameOf(a.Client),
                    TrainerName = NameOf(a.Trainer),
                    Reason = a.Reason,
                    Status = a.Status.ToString(),
                    TrainerNote = a.TrainerNote
                }).ToList();

            var model = new AppointmentReportViewModel
            {
                Rows = rows,
                IsAdmin = isAdmin,
                Days = days,
                GeneratedAt = DateTime.Now,
                Total = rows.Count,
                Pending = rows.Count(r => r.Status == "Pending"),
                Approved = rows.Count(r => r.Status == "Approved"),
                Cancelled = rows.Count(r => r.Status == "Cancelled")
            };
            return View(model);
        }

        // ---------- Workout history (client: own, trainer: own clients, admin: all) ----------
        [RoleAuthorize("Client", "Trainer", "Admin")]
        public ActionResult WorkoutReport(int days = 30)
        {
            days = NormalizeDays(days);
            string userId = User.Identity.GetUserId();
            bool isClient = User.IsInRole("Client");
            DateTime since = DateTime.Today.AddDays(-(days - 1));

            var query = db.WorkoutLogs.Include(w => w.Client).Where(w => w.LoggedOn >= since);
            if (isClient)
            {
                query = query.Where(w => w.ClientId == userId);
            }
            else if (User.IsInRole("Trainer"))
            {
                var clientIds = db.Users.Where(u => u.TrainerId == userId).Select(u => u.Id).ToList();
                query = query.Where(w => clientIds.Contains(w.ClientId));
            }

            var rows = query.OrderByDescending(w => w.LoggedOn).ThenByDescending(w => w.Id).Take(500).ToList()
                .Select(w => new WorkoutReportRow
                {
                    Date = w.LoggedOn,
                    ClientName = NameOf(w.Client),
                    Exercise = w.ExerciseName,
                    Minutes = w.DurationMinutes,
                    Calories = w.CaloriesBurned
                }).ToList();

            var model = new WorkoutReportViewModel
            {
                Rows = rows,
                IsClient = isClient,
                Days = days,
                GeneratedAt = DateTime.Now,
                TotalSessions = rows.Count,
                TotalMinutes = rows.Sum(r => r.Minutes),
                TotalCalories = rows.Sum(r => r.Calories)
            };
            return View(model);
        }

        // ---------- Helpers ----------
        private ReportViewModel BuildReport(int days)
        {
            bool isAdmin = User.IsInRole("Admin");
            string userId = User.Identity.GetUserId();

            List<ApplicationUser> clients;
            if (isAdmin)
            {
                var role = db.Roles.FirstOrDefault(r => r.Name == "Client");
                string roleId = role != null ? role.Id : "";
                clients = db.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId))
                    .OrderBy(u => u.FullName).ToList();
            }
            else
            {
                clients = db.Users.Where(u => u.TrainerId == userId)
                    .OrderBy(u => u.FullName).ToList();
            }

            var names = db.Users.ToDictionary(u => u.Id, u => string.IsNullOrEmpty(u.FullName) ? u.Email : u.FullName);

            var rows = new List<ReportRow>();
            foreach (var c in clients)
            {
                string trainerName = !string.IsNullOrEmpty(c.TrainerId) && names.ContainsKey(c.TrainerId)
                    ? names[c.TrainerId]
                    : "Unassigned";
                rows.Add(AnalyticsService.BuildReportRow(db, c, trainerName, days));
            }

            string[] categories = { "Underweight", "Normal", "Overweight", "Obese" };
            var counts = categories.Select(cat => rows.Count(r => r.BmiCategory == cat)).ToList();

            return new ReportViewModel
            {
                Rows = rows,
                IsAdmin = isAdmin,
                Days = days,
                GeneratedAt = DateTime.Now,
                TotalClients = rows.Count,
                TotalWorkouts = rows.Sum(r => r.Workouts),
                TotalBurned = rows.Sum(r => r.Burned),
                TotalEaten = rows.Sum(r => r.Eaten),
                ClientLabelsJson = AnalyticsService.ToJson(rows.Select(r => r.ClientName).ToList()),
                ClientBurnedJson = AnalyticsService.ToJson(rows.Select(r => r.Burned).ToList()),
                BmiLabelsJson = AnalyticsService.ToJson(categories),
                BmiCountsJson = AnalyticsService.ToJson(counts)
            };
        }

        private static string NameOf(ApplicationUser user)
        {
            if (user == null) return "Client";
            return string.IsNullOrEmpty(user.FullName) ? user.Email : user.FullName;
        }

        // Quotes a CSV value and blocks spreadsheet formula injection
        private static string Csv(string value)
        {
            value = value ?? "";
            if (value.Length > 0 && "=+-@".IndexOf(value[0]) >= 0)
            {
                value = "'" + value;
            }
            return "\"" + value.Replace("\"", "\"\"") + "\"";
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