using System.Collections.Generic;
using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Controllers
{
    public class BrandDashboardViewModel
    {
        public int PendingMeetings { get; set; }
        public int ApprovedMeetings { get; set; }
        public int RejectedMeetings { get; set; }
        public List<Meeting> UpcomingMeetings { get; set; } = new List<Meeting>();
    }
}