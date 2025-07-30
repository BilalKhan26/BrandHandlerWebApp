using System;
using System.ComponentModel.DataAnnotations;

namespace BrandHandlerWebApp.ViewModels
{
    public class MeetingApprovalViewModel
    {
        public int MeetingId { get; set; }
        
        [Required(ErrorMessage = "Confirmed date and time is required.")]
        [Display(Name = "Confirmed Date and Time")]
        [DataType(DataType.DateTime)]
        public DateTime ConfirmedDateTime { get; set; }
        
        [Required(ErrorMessage = "Meeting link is required.")]
        [Display(Name = "Meeting Link")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string MeetingLink { get; set; }
        
        [Display(Name = "Admin Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string Notes { get; set; }
    }
}