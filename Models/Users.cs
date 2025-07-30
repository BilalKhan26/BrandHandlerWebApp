using Microsoft.AspNetCore.Identity;
namespace BrandHandlerWebApp.Models
{
    public class Users: IdentityUser
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;
        public string ConfirmationToken { get; set; }
    }
}
