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
    }
}