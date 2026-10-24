using System.Collections.Generic;

namespace HealthTrack3.Models
{
    // Data shown on the client dashboard
    public class ClientDashboardViewModel
    {
        public string FullName { get; set; }
        public HealthMetric LatestMetric { get; set; }
        public List<WorkoutPlan> WorkoutPlans { get; set; }
        public List<DietPlan> DietPlans { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; }
        public int TodayCaloriesBurned { get; set; }
        public int TodayCaloriesEaten { get; set; }
    }
}