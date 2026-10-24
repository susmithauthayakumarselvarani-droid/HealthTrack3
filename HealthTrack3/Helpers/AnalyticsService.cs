using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using HealthTrack3.Models;

namespace HealthTrack3.Helpers
{
    /// <summary>
    /// Calculates the numbers behind the charts and reports (Task 7).
    /// </summary>
    public static class AnalyticsService
    {
        public static string ToJson(object value)
        {
            return new JavaScriptSerializer().Serialize(value);
        }

        public static string BmiCategory(double bmi)
        {
            if (bmi <= 0) { return ""; }
            if (bmi < 18.5) { return "Underweight"; }
            if (bmi < 25) { return "Normal"; }
            if (bmi < 30) { return "Overweight"; }
            return "Obese";
        }

        public static string BmiClass(string category)
        {
            switch (category)
            {
                case "Underweight": return "bmi-under";
                case "Normal": return "bmi-normal";
                case "Overweight": return "bmi-over";
                case "Obese": return "bmi-obese";
                default: return "";
            }
        }

        // Last 20 measurements + last 14 days of calories for one client
        public static ProgressChartData BuildProgress(ApplicationDbContext db, string clientId, string clientName)
        {
            DateTime today = DateTime.Today;
            DateTime start = today.AddDays(-13);

            var metrics = db.HealthMetrics
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.RecordedOn).ThenByDescending(m => m.Id)
                .Take(20)
                .ToList();
            metrics.Reverse();

            var workouts = db.WorkoutLogs.Where(w => w.ClientId == clientId && w.LoggedOn >= start).ToList();
            var meals = db.MealLogs.Where(m => m.ClientId == clientId && m.LoggedOn >= start).ToList();

            var dayLabels = new List<string>();
            var burned = new List<int>();
            var eaten = new List<int>();
            for (int i = 0; i < 14; i++)
            {
                DateTime day = start.AddDays(i);
                dayLabels.Add(day.ToString("dd MMM"));
                burned.Add(workouts.Where(w => w.LoggedOn.Date == day).Sum(w => w.CaloriesBurned));
                eaten.Add(meals.Where(m => m.LoggedOn.Date == day).Sum(m => m.Calories));
            }

            var data = new ProgressChartData
            {
                ClientName = clientName,
                HasMetrics = metrics.Any(),
                MetricCount = metrics.Count,
                BmiCategory = "",
                WorkoutCount = workouts.Count,
                TotalBurned = burned.Sum(),
                TotalEaten = eaten.Sum(),
                WeightLabelsJson = ToJson(metrics.Select(m => m.RecordedOn.ToString("dd MMM")).ToList()),
                WeightValuesJson = ToJson(metrics.Select(m => m.WeightKg).ToList()),
                BmiValuesJson = ToJson(metrics.Select(m => m.Bmi).ToList()),
                DayLabelsJson = ToJson(dayLabels),
                BurnedValuesJson = ToJson(burned),
                EatenValuesJson = ToJson(eaten)
            };

            if (metrics.Any())
            {
                var first = metrics.First();
                var latest = metrics.Last();
                data.CurrentWeight = latest.WeightKg;
                data.CurrentBmi = latest.Bmi;
                data.BmiCategory = BmiCategory(latest.Bmi);
                data.WeightChange = Math.Round(latest.WeightKg - first.WeightKg, 1);
            }
            return data;
        }

        // One line of the trainer / admin report
        public static ReportRow BuildReportRow(ApplicationDbContext db, ApplicationUser client, string trainerName, int days)
        {
            string id = client.Id;
            DateTime since = DateTime.Today.AddDays(-(days - 1));

            var latest = db.HealthMetrics.Where(m => m.ClientId == id)
                .OrderByDescending(m => m.RecordedOn).ThenByDescending(m => m.Id).FirstOrDefault();
            var earliest = db.HealthMetrics.Where(m => m.ClientId == id && m.RecordedOn >= since)
                .OrderBy(m => m.RecordedOn).ThenBy(m => m.Id).FirstOrDefault();

            var row = new ReportRow
            {
                Id = id,
                ClientName = string.IsNullOrEmpty(client.FullName) ? client.Email : client.FullName,
                Email = client.Email,
                TrainerName = trainerName,
                BmiCategory = "",
                Workouts = db.WorkoutLogs.Count(w => w.ClientId == id && w.LoggedOn >= since),
                Burned = db.WorkoutLogs.Where(w => w.ClientId == id && w.LoggedOn >= since)
                    .Select(w => (int?)w.CaloriesBurned).Sum() ?? 0,
                Eaten = db.MealLogs.Where(m => m.ClientId == id && m.LoggedOn >= since)
                    .Select(m => (int?)m.Calories).Sum() ?? 0,
                LastWorkout = db.WorkoutLogs.Where(w => w.ClientId == id)
                    .OrderByDescending(w => w.LoggedOn).Select(w => (DateTime?)w.LoggedOn).FirstOrDefault()
            };

            if (latest != null)
            {
                row.Weight = latest.WeightKg;
                row.Bmi = latest.Bmi;
                row.BmiCategory = BmiCategory(latest.Bmi);
                if (earliest != null)
                {
                    row.WeightChange = Math.Round(latest.WeightKg - earliest.WeightKg, 1);
                }
            }
            return row;
        }
    }
}