using System;
using System.Collections.Generic;

namespace HealthTrack3.Models
{
    public class TrainerClientRow
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public double? LatestWeight { get; set; }
        public double? LatestBmi { get; set; }
        public DateTime? LastWorkout { get; set; }
    }

    public class TrainerDashboardViewModel
    {
        public string FullName { get; set; }
        public List<TrainerClientRow> Clients { get; set; }
        public int PendingAppointments { get; set; }
    }

    public class ClientProgressViewModel
    {
        public ApplicationUser Client { get; set; }
        public List<HealthMetric> Metrics { get; set; }
        public List<WorkoutLog> Workouts { get; set; }
        public List<MealLog> Meals { get; set; }
        public List<WorkoutPlan> WorkoutPlans { get; set; }
        public List<DietPlan> DietPlans { get; set; }
        public List<Feedback> Feedbacks { get; set; }
    }
}