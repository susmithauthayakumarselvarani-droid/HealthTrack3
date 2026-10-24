using System;
using System.Collections.Generic;

namespace HealthTrack3.Models
{
    // Data behind the progress charts of one client (Task 7)
    public class ProgressChartData
    {
        public string ClientName { get; set; }
        public int Days { get; set; }
        public bool HasMetrics { get; set; }
        public int MetricCount { get; set; }

        public double CurrentWeight { get; set; }
        public double WeightChange { get; set; }
        public double CurrentBmi { get; set; }
        public string BmiCategory { get; set; }

        public int WorkoutCount { get; set; }
        public int TotalBurned { get; set; }
        public int TotalEaten { get; set; }

        // JSON text written straight into the Chart.js script
        public string WeightLabelsJson { get; set; }
        public string WeightValuesJson { get; set; }
        public string BmiValuesJson { get; set; }
        public string DayLabelsJson { get; set; }
        public string BurnedValuesJson { get; set; }
        public string EatenValuesJson { get; set; }
    }

    // One client line in the trainer / admin report
    public class ReportRow
    {
        public string Id { get; set; }
        public string ClientName { get; set; }
        public string Email { get; set; }
        public string TrainerName { get; set; }
        public double? Weight { get; set; }
        public double? Bmi { get; set; }
        public string BmiCategory { get; set; }
        public double? WeightChange { get; set; }
        public int Workouts { get; set; }
        public int Burned { get; set; }
        public int Eaten { get; set; }
        public DateTime? LastWorkout { get; set; }
    }

    public class ReportViewModel
    {
        public List<ReportRow> Rows { get; set; }
        public bool IsAdmin { get; set; }
        public int Days { get; set; }
        public DateTime GeneratedAt { get; set; }

        public int TotalClients { get; set; }
        public int TotalWorkouts { get; set; }
        public int TotalBurned { get; set; }
        public int TotalEaten { get; set; }

        public string ClientLabelsJson { get; set; }
        public string ClientBurnedJson { get; set; }
        public string BmiLabelsJson { get; set; }
        public string BmiCountsJson { get; set; }
    }

    // Appointment report (trainer / admin)
    public class AppointmentReportRow
    {
        public DateTime When { get; set; }
        public string ClientName { get; set; }
        public string TrainerName { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string TrainerNote { get; set; }
    }

    public class AppointmentReportViewModel
    {
        public List<AppointmentReportRow> Rows { get; set; }
        public bool IsAdmin { get; set; }
        public int Days { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Cancelled { get; set; }
    }

    // Workout history report (client sees own, trainer / admin see clients)
    public class WorkoutReportRow
    {
        public DateTime Date { get; set; }
        public string ClientName { get; set; }
        public string Exercise { get; set; }
        public int Minutes { get; set; }
        public int Calories { get; set; }
    }

    public class WorkoutReportViewModel
    {
        public List<WorkoutReportRow> Rows { get; set; }
        public bool IsClient { get; set; }
        public int Days { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalSessions { get; set; }
        public int TotalMinutes { get; set; }
        public int TotalCalories { get; set; }
    }
}