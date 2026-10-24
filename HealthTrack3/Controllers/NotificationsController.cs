using System;
using System.Linq;
using System.Web.Mvc;
using HealthTrack3.Helpers;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;

namespace HealthTrack3.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ---------- Notification list (Task 8) ----------
        public ActionResult Index()
        {
            string userId = User.Identity.GetUserId();
            var list = db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(50)
                .ToList();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkRead(int id)
        {
            string userId = User.Identity.GetUserId();
            var note = db.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);
            if (note != null && !note.IsRead)
            {
                note.IsRead = true;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllRead()
        {
            string userId = User.Identity.GetUserId();
            var unread = db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
            foreach (var note in unread)
            {
                note.IsRead = true;
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            string userId = User.Identity.GetUserId();
            var note = db.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId);
            if (note != null)
            {
                db.Notifications.Remove(note);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ---------- Bell icon in the navbar ----------
        // Also creates today's reminders for clients and trainers.
        // Returns plain HTML, so no separate view file is needed.
        [ChildActionOnly]
        public ActionResult Bell()
        {
            int unread = 0;
            try
            {
                string userId = User.Identity.GetUserId();
                ReminderService.Generate(db, userId, User.IsInRole("Client"), User.IsInRole("Trainer"));
                unread = db.Notifications.Count(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception)
            {
                // A reminder problem must never break the page
                unread = 0;
            }

            string badge = unread > 0
                ? "<span class=\"bell-badge\">" + (unread > 99 ? "99+" : unread.ToString()) + "</span>"
                : "";

            string html = "<a href=\"" + Url.Action("Index", "Notifications") + "\" class=\"bell-link\" title=\"Notifications\">"
                        + "<i class=\"fa-solid fa-bell\"></i>" + badge + "</a>";

            return Content(html);
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