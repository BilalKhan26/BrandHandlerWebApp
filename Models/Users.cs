using Microsoft.AspNetCore.Identity;
namespace BrandHandlerWebApp.Models
{
    public class Users: IdentityUser
    {
        public string FullName { get; set; }
    }
}
