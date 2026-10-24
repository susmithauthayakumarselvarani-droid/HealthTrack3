namespace HealthTrack3.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddHealthModels1 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            CreateTable(
                "dbo.Appointments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        TrainerId = c.String(nullable: false, maxLength: 128),
                        AppointmentDate = c.DateTime(nullable: false),
                        Reason = c.String(nullable: false, maxLength: 300),
                        Status = c.Int(nullable: false),
                        TrainerNote = c.String(maxLength: 300),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .ForeignKey("dbo.AspNetUsers", t => t.TrainerId)
                .Index(t => t.ClientId)
                .Index(t => t.TrainerId);
            
            CreateTable(
                "dbo.DietPlans",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TrainerId = c.String(nullable: false, maxLength: 128),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        Title = c.String(nullable: false, maxLength: 100),
                        Meals = c.String(nullable: false, maxLength: 1000),
                        DailyCalorieTarget = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .ForeignKey("dbo.AspNetUsers", t => t.TrainerId)
                .Index(t => t.TrainerId)
                .Index(t => t.ClientId);
            
            CreateTable(
                "dbo.Feedbacks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TrainerId = c.String(nullable: false, maxLength: 128),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        Message = c.String(nullable: false, maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .ForeignKey("dbo.AspNetUsers", t => t.TrainerId)
                .Index(t => t.TrainerId)
                .Index(t => t.ClientId);
            
            CreateTable(
                "dbo.HealthMetrics",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        RecordedOn = c.DateTime(nullable: false),
                        WeightKg = c.Double(nullable: false),
                        HeightCm = c.Double(nullable: false),
                        Bmi = c.Double(nullable: false),
                        CaloriesBurned = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .Index(t => t.ClientId);
            
            CreateTable(
                "dbo.MealLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        LoggedOn = c.DateTime(nullable: false),
                        MealType = c.String(nullable: false, maxLength: 30),
                        FoodName = c.String(nullable: false, maxLength: 100),
                        Calories = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .Index(t => t.ClientId);
            
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        Message = c.String(nullable: false, maxLength: 300),
                        Type = c.String(maxLength: 30),
                        CreatedAt = c.DateTime(nullable: false),
                        IsRead = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.WorkoutLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        LoggedOn = c.DateTime(nullable: false),
                        ExerciseName = c.String(nullable: false, maxLength: 100),
                        DurationMinutes = c.Int(nullable: false),
                        CaloriesBurned = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .Index(t => t.ClientId);
            
            CreateTable(
                "dbo.WorkoutPlans",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TrainerId = c.String(nullable: false, maxLength: 128),
                        ClientId = c.String(nullable: false, maxLength: 128),
                        Title = c.String(nullable: false, maxLength: 100),
                        Exercises = c.String(nullable: false, maxLength: 1000),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ClientId)
                .ForeignKey("dbo.AspNetUsers", t => t.TrainerId)
                .Index(t => t.TrainerId)
                .Index(t => t.ClientId);
            
            AddColumn("dbo.AspNetUsers", "TrainerId", c => c.String());
            AddForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles", "Id");
            AddForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers", "Id");
            AddForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.WorkoutPlans", "TrainerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.WorkoutPlans", "ClientId", "dbo.AspNetUsers");
            DropForeignKey("dbo.WorkoutLogs", "ClientId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Notifications", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.MealLogs", "ClientId", "dbo.AspNetUsers");
            DropForeignKey("dbo.HealthMetrics", "ClientId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Feedbacks", "TrainerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Feedbacks", "ClientId", "dbo.AspNetUsers");
            DropForeignKey("dbo.DietPlans", "TrainerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.DietPlans", "ClientId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Appointments", "TrainerId", "dbo.AspNetUsers");
            DropForeignKey("dbo.Appointments", "ClientId", "dbo.AspNetUsers");
            DropIndex("dbo.WorkoutPlans", new[] { "ClientId" });
            DropIndex("dbo.WorkoutPlans", new[] { "TrainerId" });
            DropIndex("dbo.WorkoutLogs", new[] { "ClientId" });
            DropIndex("dbo.Notifications", new[] { "UserId" });
            DropIndex("dbo.MealLogs", new[] { "ClientId" });
            DropIndex("dbo.HealthMetrics", new[] { "ClientId" });
            DropIndex("dbo.Feedbacks", new[] { "ClientId" });
            DropIndex("dbo.Feedbacks", new[] { "TrainerId" });
            DropIndex("dbo.DietPlans", new[] { "ClientId" });
            DropIndex("dbo.DietPlans", new[] { "TrainerId" });
            DropIndex("dbo.Appointments", new[] { "TrainerId" });
            DropIndex("dbo.Appointments", new[] { "ClientId" });
            DropColumn("dbo.AspNetUsers", "TrainerId");
            DropTable("dbo.WorkoutPlans");
            DropTable("dbo.WorkoutLogs");
            DropTable("dbo.Notifications");
            DropTable("dbo.MealLogs");
            DropTable("dbo.HealthMetrics");
            DropTable("dbo.Feedbacks");
            DropTable("dbo.DietPlans");
            DropTable("dbo.Appointments");
            AddForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles", "Id", cascadeDelete: true);
        }
    }
}
