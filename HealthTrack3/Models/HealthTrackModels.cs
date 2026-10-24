using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthTrack3.Models
{
    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Cancelled
    }

    // Weight, height, BMI and calories entered by a client (Task 5, 7)
    public class HealthMetric
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime RecordedOn { get; set; } = DateTime.Today;

        [Range(20, 400, ErrorMessage = "Weight must be between 20 and 400 kg")]
        [Display(Name = "Weight (kg)")]
        public double WeightKg { get; set; }

        [Range(50, 260, ErrorMessage = "Height must be between 50 and 260 cm")]
        [Display(Name = "Height (cm)")]
        public double HeightCm { get; set; }

        public double Bmi { get; set; }

        [Range(0, 20000)]
        [Display(Name = "Calories burned")]
        public int CaloriesBurned { get; set; }
    }

    // Workout plan created by a trainer for a client (Task 5)
    public class WorkoutPlan
    {
        public int Id { get; set; }

        [Required]
        public string TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; }

        [Required, StringLength(1000)]
        public string Exercises { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    }

    // Exercise session logged by a client (Task 5)
    public class WorkoutLog
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [DataType(DataType.Date)]
        public DateTime LoggedOn { get; set; } = DateTime.Today;

        [Required, StringLength(100)]
        [Display(Name = "Exercise")]
        public string ExerciseName { get; set; }

        [Range(1, 600)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [Range(0, 5000)]
        [Display(Name = "Calories burned")]
        public int CaloriesBurned { get; set; }
    }

    // Diet plan created by a trainer for a client (Task 5)
    public class DietPlan
    {
        public int Id { get; set; }

        [Required]
        public string TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; }

        [Required, StringLength(1000)]
        public string Meals { get; set; }

        [Range(500, 6000)]
        [Display(Name = "Daily calorie target")]
        public int DailyCalorieTarget { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    }

    // Meal logged by a client (Task 5)
    public class MealLog
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [DataType(DataType.Date)]
        public DateTime LoggedOn { get; set; } = DateTime.Today;

        [Required, StringLength(30)]
        [Display(Name = "Meal type")]
        public string MealType { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Food")]
        public string FoodName { get; set; }

        [Range(0, 5000)]
        public int Calories { get; set; }
    }

    // Consultation booked by a client with a trainer (Task 6)
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [Required]
        public string TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Appointment time")]
        public DateTime AppointmentDate { get; set; }

        [Required, StringLength(300)]
        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(300)]
        [Display(Name = "Trainer note")]
        public string TrainerNote { get; set; }
    }

    // Feedback given by a trainer to a client (Task 3)
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        public string TrainerId { get; set; }
        [ForeignKey("TrainerId")]
        public virtual ApplicationUser Trainer { get; set; }

        [Required]
        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }

        [Required, StringLength(500)]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // Reminder shown to a user (Task 8)
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required, StringLength(300)]
        public string Message { get; set; }

        [StringLength(30)]
        public string Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; }
    }
}