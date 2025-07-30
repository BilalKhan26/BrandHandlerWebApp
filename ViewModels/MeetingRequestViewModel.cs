using System;
using System.ComponentModel.DataAnnotations;

namespace BrandHandlerWebApp.ViewModels
{
    public class MeetingRequestViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Requested date and time is required.")]
        [Display(Name = "Requested Date and Time")]
        [DataType(DataType.DateTime)]
        public DateTime RequestedDateTime { get; set; }
    }
}