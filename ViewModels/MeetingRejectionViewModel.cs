using System.ComponentModel.DataAnnotations;

namespace BrandHandlerWebApp.ViewModels
{
    public class MeetingRejectionViewModel
    {
        public int MeetingId { get; set; }

        [Required(ErrorMessage = "Reason for rejection is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string RejectionReason { get; set; }
    }
}