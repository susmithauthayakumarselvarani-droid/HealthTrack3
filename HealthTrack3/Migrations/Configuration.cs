using System.Data.Entity.Migrations;
using HealthTrack3.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HealthTrack3.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
            // 1. Create the three roles
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            string[] roles = { "Admin", "Trainer", "Client" };

            foreach (string role in roles)
            {
                if (!roleManager.RoleExists(role))
                {
                    roleManager.Create(new IdentityRole(role));
                }
            }

            // 2. Create a default admin user
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            string adminEmail = "admin@healthtrack.com";

            if (userManager.FindByEmail(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    EmailConfirmed = true
                };

                var result = userManager.Create(admin, "Admin@123");
                if (result.Succeeded)
                {
                    userManager.AddToRole(admin.Id, "Admin");
                }
            }
        }
    }
}