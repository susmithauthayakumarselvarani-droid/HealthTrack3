using System;
using System.Linq;
using HealthTrack3.Models;

namespace HealthTrack3.Helpers
{
    /// <summary>
    /// Creates reminder notifications (Task 8) for workouts, meals and appointments.
    /// Each reminder is created only once, so users never get duplicates.
    /// </summary>
    public static class ReminderService
    {
        public static void Generate(ApplicationDbContext db, string userId, bool isClient, bool isTrainer)
        {
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            DateTime in24Hours = now.AddHours(24);

            if (isClient)
            {
                bool workedOutToday = db.WorkoutLogs
                    .Any(w => w.ClientId == userId && w.LoggedOn >= today && w.LoggedOn < tomorrow);
                if (!workedOutToday)
                {
                    AddDaily(db, userId, "WorkoutReminder",
                        "Workout reminder: you have not logged a workout today.", today);
                }

                bool ateToday = db.MealLogs
                    .Any(m => m.ClientId == userId && m.LoggedOn >= today && m.LoggedOn < tomorrow);
                if (!ateToday)
                {
                    AddDaily(db, userId, "MealReminder",
                        "Meal reminder: remember to log your meals today.", today);
                }
            }

            if (isClient || isTrainer)
            {
                // Appointments happening within the next 24 hours
                var upcoming = isClient
                    ? db.Appointments.Where(a => a.ClientId == userId
                                                 && a.Status != AppointmentStatus.Cancelled
                                                 && a.AppointmentDate > now
                                                 && a.AppointmentDate <= in24Hours)
                    : db.Appointments.Where(a => a.TrainerId == userId
                                                 && a.Status != AppointmentStatus.Cancelled
                                                 && a.AppointmentDate > now
                                                 && a.AppointmentDate <= in24Hours);

                var times = upcoming.Select(a => a.AppointmentDate).ToList();
                foreach (var time in times)
                {
                    string message = "Reminder: you have an appointment on "
                                     + time.ToString("dd MMM yyyy, hh:mm tt") + ".";
                    bool exists = db.Notifications.Any(n => n.UserId == userId
                                                            && n.Type == "AppointmentReminder"
                                                            && n.Message == message);
                    if (!exists)
                    {
                        db.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Type = "AppointmentReminder",
                            Message = message
                        });
                    }
                }
            }

            if (isTrainer)
            {
                int pending = db.Appointments.Count(a => a.TrainerId == userId
                                                         && a.Status == AppointmentStatus.Pending);
                if (pending > 0)
                {
                    AddDaily(db, userId, "PendingReminder",
                        "You have " + pending + " pending appointment request(s) waiting for your response.", today);
                }
            }

            db.SaveChanges();
        }

        // Adds a reminder only if the same type has not been created today
        private static void AddDaily(ApplicationDbContext db, string userId, string type, string message, DateTime today)
        {
            bool alreadyToday = db.Notifications
                .Any(n => n.UserId == userId && n.Type == type && n.CreatedAt >= today);
            if (!alreadyToday)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Type = type,
                    Message = message
                });
            }
        }
    }
}