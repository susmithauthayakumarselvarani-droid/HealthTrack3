using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using HealthTrack3.Helpers;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;

namespace HealthTrack3.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // Appointments start on the hour, 9:00 AM to 4:00 PM (each lasts one hour)
        private static readonly int[] SlotHours = { 9, 10, 11, 12, 13, 14, 15, 16 };

        // ================= CLIENT SIDE =================

        // Booking page + the client's own appointment list
        [RoleAuthorize("Client")]
        public ActionResult My()
        {
            LoadClientData();
            return View(new Appointment());
        }

        [HttpPost]
        [RoleAuthorize("Client")]
        [ValidateAntiForgeryToken]
        public ActionResult My(string trainerId, string date, string slot, string reason)
        {
            string clientId = User.Identity.GetUserId();
            DateTime when;
            string error = ValidateSlot(trainerId, date, slot, 0, out when);

            if (error == null && string.IsNullOrWhiteSpace(reason))
            {
                error = "Please enter the reason for the appointment.";
            }
            if (error == null && reason.Length > 300)
            {
                error = "The reason can be at most 300 characters.";
            }

            if (error != null)
            {
                ModelState.AddModelError("", error);
                LoadClientData();
                ViewBag.SelectedDate = date;
                ViewBag.SelectedSlot = slot;
                return View(new Appointment { TrainerId = trainerId, Reason = reason });
            }

            try
            {
                var client = db.Users.FirstOrDefault(u => u.Id == clientId);
                string clientName = client != null && !string.IsNullOrEmpty(client.FullName) ? client.FullName : User.Identity.Name;

                db.Appointments.Add(new Appointment
                {
                    ClientId = clientId,
                    TrainerId = trainerId,
                    AppointmentDate = when,
                    Reason = reason.Trim(),
                    Status = AppointmentStatus.Pending
                });
                db.Notifications.Add(new Notification
                {
                    UserId = trainerId,
                    Type = "Appointment",
                    Message = "New appointment request from " + clientName + " for " + when.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture)
                });
                db.SaveChanges();

                TempData["Success"] = "Booking received for " + when.ToString("dddd, dd MMM yyyy 'at' hh:mm tt", CultureInfo.InvariantCulture)
                                      + ". Your trainer will confirm it soon.";
                return RedirectToAction("My");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Could not book the appointment. Please try again.");
                LoadClientData();
                ViewBag.SelectedDate = date;
                ViewBag.SelectedSlot = slot;
                return View(new Appointment { TrainerId = trainerId, Reason = reason });
            }
        }

        // Free / taken time slots of a trainer on one day (used by the booking page)
        [RoleAuthorize("Client")]
        public ActionResult Slots(string trainerId, string date, int ignoreId = 0)
        {
            DateTime day;
            if (string.IsNullOrEmpty(trainerId)
                || !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
            {
                return Json(new { slots = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            DateTime dayStart = day.Date;
            DateTime dayEnd = dayStart.AddDays(1);
            var booked = db.Appointments
                .Where(a => a.TrainerId == trainerId
                            && a.Status != AppointmentStatus.Cancelled
                            && a.Id != ignoreId
                            && a.AppointmentDate >= dayStart
                            && a.AppointmentDate < dayEnd)
                .Select(a => a.AppointmentDate)
                .ToList();

            var slots = SlotHours.Select(h =>
            {
                DateTime start = dayStart.AddHours(h);
                bool available = start > DateTime.Now && !booked.Any(b => b.Hour == h);
                return new
                {
                    time = h.ToString("00") + ":00",
                    label = start.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                    available = available
                };
            }).ToList();

            return Json(new { slots = slots }, JsonRequestBehavior.AllowGet);
        }

        // Client asks to move an appointment to another slot (trainer must approve again)
        [RoleAuthorize("Client")]
        public ActionResult Reschedule(int id)
        {
            var appt = GetClientAppointment(id);
            if (appt == null) return Redirect("~/Home/AccessDenied");
            if (appt.Status == AppointmentStatus.Cancelled || appt.AppointmentDate <= DateTime.Now)
            {
                TempData["Error"] = "This appointment can no longer be rescheduled.";
                return RedirectToAction("My");
            }
            return View(appt);
        }

        [HttpPost]
        [RoleAuthorize("Client")]
        [ValidateAntiForgeryToken]
        public ActionResult Reschedule(int id, string date, string slot)
        {
            var appt = GetClientAppointment(id);
            if (appt == null) return Redirect("~/Home/AccessDenied");
            if (appt.Status == AppointmentStatus.Cancelled || appt.AppointmentDate <= DateTime.Now)
            {
                TempData["Error"] = "This appointment can no longer be rescheduled.";
                return RedirectToAction("My");
            }

            DateTime when;
            string error = ValidateSlot(appt.TrainerId, date, slot, id, out when);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                ViewBag.SelectedDate = date;
                ViewBag.SelectedSlot = slot;
                return View(appt);
            }

            try
            {
                string clientId = User.Identity.GetUserId();
                var client = db.Users.FirstOrDefault(u => u.Id == clientId);
                string clientName = client != null && !string.IsNullOrEmpty(client.FullName) ? client.FullName : User.Identity.Name;
                DateTime oldTime = appt.AppointmentDate;

                appt.AppointmentDate = when;
                appt.Status = AppointmentStatus.Pending;
                db.Notifications.Add(new Notification
                {
                    UserId = appt.TrainerId,
                    Type = "Appointment",
                    Message = clientName + " asked to move the appointment from "
                              + oldTime.ToString("dd MMM, hh:mm tt", CultureInfo.InvariantCulture) + " to "
                              + when.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) + ". Please approve it again."
                });
                db.SaveChanges();

                TempData["Success"] = "Reschedule requested for " + when.ToString("dddd, dd MMM yyyy 'at' hh:mm tt", CultureInfo.InvariantCulture)
                                      + ". It is pending until your trainer approves it.";
                return RedirectToAction("My");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Could not reschedule the appointment. Please try again.");
                ViewBag.SelectedDate = date;
                ViewBag.SelectedSlot = slot;
                return View(appt);
            }
        }

        [HttpPost]
        [RoleAuthorize("Client")]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            string clientId = User.Identity.GetUserId();
            var appt = db.Appointments.FirstOrDefault(a => a.Id == id && a.ClientId == clientId);
            if (appt == null) return Redirect("~/Home/AccessDenied");

            if (appt.Status != AppointmentStatus.Cancelled)
            {
                try
                {
                    appt.Status = AppointmentStatus.Cancelled;
                    db.Notifications.Add(new Notification
                    {
                        UserId = appt.TrainerId,
                        Type = "Appointment",
                        Message = "A client cancelled the appointment on " + appt.AppointmentDate.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture)
                    });
                    db.SaveChanges();
                    TempData["Success"] = "Appointment cancelled.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Could not cancel the appointment. Please try again.";
                }
            }
            return RedirectToAction("My");
        }

        // ================= TRAINER SIDE =================

        [RoleAuthorize("Trainer")]
        public ActionResult Manage()
        {
            string trainerId = User.Identity.GetUserId();
            var list = db.Appointments.Include(a => a.Client)
                .Where(a => a.TrainerId == trainerId)
                .OrderBy(a => a.Status)
                .ThenBy(a => a.AppointmentDate)
                .ToList();
            return View(list);
        }

        [HttpPost]
        [RoleAuthorize("Trainer")]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            var appt = GetTrainerAppointment(id);
            if (appt == null) return Redirect("~/Home/AccessDenied");

            if (appt.Status == AppointmentStatus.Pending)
            {
                try
                {
                    appt.Status = AppointmentStatus.Approved;
                    db.Notifications.Add(new Notification
                    {
                        UserId = appt.ClientId,
                        Type = "Appointment",
                        Message = "Your appointment on " + appt.AppointmentDate.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) + " was approved."
                    });
                    db.SaveChanges();
                    TempData["Success"] = "Appointment approved.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Could not approve the appointment. Please try again.";
                }
            }
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [RoleAuthorize("Trainer")]
        [ValidateAntiForgeryToken]
        public ActionResult TrainerCancel(int id)
        {
            var appt = GetTrainerAppointment(id);
            if (appt == null) return Redirect("~/Home/AccessDenied");

            if (appt.Status != AppointmentStatus.Cancelled)
            {
                try
                {
                    appt.Status = AppointmentStatus.Cancelled;
                    db.Notifications.Add(new Notification
                    {
                        UserId = appt.ClientId,
                        Type = "Appointment",
                        Message = "Your appointment on " + appt.AppointmentDate.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture) + " was cancelled by your trainer."
                    });
                    db.SaveChanges();
                    TempData["Success"] = "Appointment cancelled.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Could not cancel the appointment. Please try again.";
                }
            }
            return RedirectToAction("Manage");
        }

        [RoleAuthorize("Trainer")]
        public ActionResult Update(int id)
        {
            var appt = GetTrainerAppointment(id);
            if (appt == null) return Redirect("~/Home/AccessDenied");
            if (appt.Status == AppointmentStatus.Cancelled) return RedirectToAction("Manage");

            return View(appt);
        }

        [HttpPost]
        [RoleAuthorize("Trainer")]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id, DateTime? appointmentDate, string trainerNote)
        {
            var appt = GetTrainerAppointment(id);
            if (appt == null) return Redirect("~/Home/AccessDenied");
            if (appt.Status == AppointmentStatus.Cancelled) return RedirectToAction("Manage");

            if (!appointmentDate.HasValue || appointmentDate.Value <= DateTime.Now)
            {
                ModelState.AddModelError("", "Please choose a future date and time.");
            }
            if (!string.IsNullOrEmpty(trainerNote) && trainerNote.Length > 300)
            {
                ModelState.AddModelError("", "The note can be at most 300 characters.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    appt.AppointmentDate = appointmentDate.Value;
                    appt.TrainerNote = string.IsNullOrWhiteSpace(trainerNote) ? null : trainerNote.Trim();
                    db.Notifications.Add(new Notification
                    {
                        UserId = appt.ClientId,
                        Type = "Appointment",
                        Message = "Your appointment was updated. New time: " + appt.AppointmentDate.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture)
                    });
                    db.SaveChanges();

                    TempData["Success"] = "Appointment updated.";
                    return RedirectToAction("Manage");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Could not update the appointment. Please try again.");
                }
            }
            return View(appt);
        }

        // ================= HELPERS =================

        // Checks trainer, date, slot, future time and double booking. Returns an error text or null.
        private string ValidateSlot(string trainerId, string date, string slot, int ignoreId, out DateTime when)
        {
            when = DateTime.MinValue;

            if (string.IsNullOrEmpty(trainerId) || !GetTrainers().Any(t => t.Id == trainerId))
            {
                return "Please choose a trainer.";
            }

            DateTime day;
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
            {
                return "Please choose a date.";
            }

            TimeSpan time;
            if (!TimeSpan.TryParseExact(slot ?? "", "hh\\:mm", CultureInfo.InvariantCulture, out time)
                || time.Minutes != 0
                || !SlotHours.Contains(time.Hours))
            {
                return "Please choose a time slot.";
            }

            when = day.Date + time;
            if (when <= DateTime.Now)
            {
                return "Please choose a future time.";
            }
            if (IsSlotTaken(trainerId, when, ignoreId))
            {
                return "Sorry, that time slot is already booked. Please choose another one.";
            }
            return null;
        }

        // A slot is taken if the trainer has a non-cancelled appointment in the same hour
        private bool IsSlotTaken(string trainerId, DateTime when, int ignoreId)
        {
            DateTime from = new DateTime(when.Year, when.Month, when.Day, when.Hour, 0, 0);
            DateTime to = from.AddHours(1);
            return db.Appointments.Any(a => a.TrainerId == trainerId
                                            && a.Status != AppointmentStatus.Cancelled
                                            && a.Id != ignoreId
                                            && a.AppointmentDate >= from
                                            && a.AppointmentDate < to);
        }

        // All users in the Trainer role
        private List<ApplicationUser> GetTrainers()
        {
            var role = db.Roles.FirstOrDefault(r => r.Name == "Trainer");
            if (role == null) return new List<ApplicationUser>();

            string roleId = role.Id;
            return db.Users
                .Where(u => u.Roles.Any(r => r.RoleId == roleId))
                .OrderBy(u => u.FullName)
                .ToList();
        }

        private void LoadClientData()
        {
            string clientId = User.Identity.GetUserId();

            ViewBag.Trainers = GetTrainers()
                .Select(t => new SelectListItem
                {
                    Value = t.Id,
                    Text = string.IsNullOrEmpty(t.FullName) ? t.Email : t.FullName
                })
                .ToList();

            ViewBag.History = db.Appointments.Include(a => a.Trainer)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToList();
        }

        // Returns the appointment only if it belongs to the logged-in client
        private Appointment GetClientAppointment(int id)
        {
            string clientId = User.Identity.GetUserId();
            return db.Appointments.Include(a => a.Trainer)
                .FirstOrDefault(a => a.Id == id && a.ClientId == clientId);
        }

        // Returns the appointment only if it belongs to the logged-in trainer
        private Appointment GetTrainerAppointment(int id)
        {
            string trainerId = User.Identity.GetUserId();
            return db.Appointments.Include(a => a.Client)
                .FirstOrDefault(a => a.Id == id && a.TrainerId == trainerId);
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