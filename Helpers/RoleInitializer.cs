using BrandHandlerWebApp.Data;
using BrandHandlerWebApp.Models;
using Microsoft.AspNetCore.Identity;

namespace BrandHandlerWebApp.Helpers
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Users>>();
            
            // Create roles if they don't exist
            await CreateRoleIfNotExistsAsync(roleManager, Constants.AdminRole);
            await CreateRoleIfNotExistsAsync(roleManager, Constants.BrandRole);
            
            // Create admin user if it doesn't exist
            await CreateAdminUserIfNotExistsAsync(userManager);
        }
        
        private static async Task CreateRoleIfNotExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private static async Task CreateAdminUserIfNotExistsAsync(UserManager<Users> userManager)
        {
            var adminEmail = "admin@brandhandler.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new Users
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Admin User",
                    EmailConfirmed = true,
                    Role = Constants.AdminRole
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Constants.AdminRole);
                }
            }
        }

        public static async Task SeedMeetingsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Users>>();

            if (!context.Meetings.Any())
            {
                var brandUser = await userManager.FindByEmailAsync("brand@example.com");
                if (brandUser == null)
                {
                    brandUser = new Users
                    {
                        UserName = "brand@example.com",
                        Email = "brand@example.com",
                        FullName = "Brand User",
                        EmailConfirmed = true,
                        Role = Constants.BrandRole
                    };
                    var result = await userManager.CreateAsync(brandUser, "Brand@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(brandUser, Constants.BrandRole);
                    }
                }

                if (brandUser != null)
                {
                    context.Meetings.AddRange(
                        new Meeting
                        {
                            Title = "Product Launch Discussion",
                            Description = "Discussing marketing strategies for new product launch.",
                            RequestedDateTime = DateTime.Now.AddDays(7),
                            BrandUserId = brandUser.Id,
                            Status = Constants.MeetingStatusPending
                        },
                        new Meeting
                        {
                            Title = "Q3 Performance Review",
                            Description = "Reviewing Q3 sales and marketing performance.",
                            RequestedDateTime = DateTime.Now.AddDays(-10),
                            ConfirmedDateTime = DateTime.Now.AddDays(-9),
                            BrandUserId = brandUser.Id,
                            Status = Constants.MeetingStatusApproved,
                            MeetingLink = "https://meet.google.com/q3-review"
                        },
                        new Meeting
                        {
                            Title = "Website Redesign Feedback",
                            Description = "Gathering feedback on the new website design.",
                            RequestedDateTime = DateTime.Now.AddDays(-5),
                            BrandUserId = brandUser.Id,
                            Status = Constants.MeetingStatusRejected,
                            AdminNotes = "Brand provided insufficient details for the meeting."
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
