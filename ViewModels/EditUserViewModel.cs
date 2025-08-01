using System.ComponentModel.DataAnnotations;

namespace BrandHandlerWebApp.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } // User ID

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }
    }

}
