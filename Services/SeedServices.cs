using Microsoft.AspNetCore.Identity;
using BrandHandlerWebApp.Data;
using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Services
{
    public class SeedServices
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedServices>>();

            try
            {
                logger.LogInformation("Ensuring the database is created");
                await context.Database.EnsureCreatedAsync();

                logger.LogInformation("Seeding rules");
                await AddRoleAsync(roleManager, "Admin");
                await AddRoleAsync(roleManager, "User");

                logger.LogInformation("Seeding admin user");
                var AdminEmail = "admin@gmail.com";
                if (await userManager.FindByEmailAsync(AdminEmail)==null)
                {
                    var AdminUser = new Users
                    {
                        FullName = "Admin",
                        UserName = AdminEmail,
                        NormalizedUserName = AdminEmail.ToUpper(),
                        Email = AdminEmail,
                        NormalizedEmail = AdminEmail.ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    var result = await userManager.CreateAsync(AdminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Assigning Admin Roles to Admin Users.");
                            await userManager.AddToRoleAsync(AdminUser, "Admin");
                    }
                    else
                    {
                        logger.LogError("Failed to Create Admin User: {Errors}",string.Join(",",result.Errors.Select(e=>e.Description)));
                    }
                }
             
            
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
        
        public static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager,string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if(!result.Succeeded)
                {
                    throw new Exception($"Failed to create role '{roleName}':{string.Join(",", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
