using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandHandlerWebApp.Models
{
    public class Meeting
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        
        public string? Title { get; set; }
        
        [Required]
        public string? Description { get; set; }
        
        [Required]
        public DateTime? RequestedDateTime { get; set; }
        
        public DateTime? ConfirmedDateTime { get; set; }
        
        [Required]
        public string? BrandUserId { get; set; }
        
        [ForeignKey("BrandUserId")]
        public Users? BrandUser { get; set; }
        
        public string? AdminUserId { get; set; }
        
        [ForeignKey("AdminUserId")]
        public Users? AdminUser { get; set; }
        
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        public string? MeetingLink { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set;}

        public int NotificationCount { get; set; } = 0;

        public string? AdminNotes { get; set; }
    }
}