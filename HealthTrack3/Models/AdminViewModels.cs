using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HealthTrack3.Models
{
    public class AdminUserRow
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string TrainerId { get; set; }
        public string TrainerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsLocked { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int Clients { get; set; }
        public int Trainers { get; set; }
        public int NewUsersLast7Days { get; set; }
        public int UnassignedClients { get; set; }

        public int PendingAppointments { get; set; }
        public int ApprovedAppointments { get; set; }
        public int CancelledAppointments { get; set; }

        public int WorkoutLogs { get; set; }
        public int MealLogs { get; set; }
        public int Measurements { get; set; }

        public List<AdminUserRow> RecentUsers { get; set; }
    }

    public class AdminUsersViewModel
    {
        public List<AdminUserRow> Users { get; set; }
        public List<SelectListItem> Trainers { get; set; }
        public string RoleFilter { get; set; }
        public string Search { get; set; }
    }

    public class AdminTrainerRow
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int ClientCount { get; set; }
        public int PendingAppointments { get; set; }
        public int TotalAppointments { get; set; }
        public bool IsLocked { get; set; }
    }
}