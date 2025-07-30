using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrandHandlerWebApp.Data;
using BrandHandlerWebApp.Helpers;
using BrandHandlerWebApp.Models;
using BrandHandlerWebApp.ViewModels;

namespace BrandHandlerWebApp.Controllers
{
    [Authorize(Roles = "Brand")]
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<BrandController> _logger;

        public BrandController(
            AppDbContext context,
            UserManager<Users> userManager,
            ILogger<BrandController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var meetings = await _context.Meetings
                .Where(m => m.BrandUserId == currentUser.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(meetings);
        }

        [HttpGet]
        public IActionResult ScheduleMeeting()
        {
            return View(new MeetingRequestViewModel
            {
                RequestedDateTime = DateTime.Now.AddDays(1).Date.AddHours(9) // Default to 9 AM tomorrow
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleMeeting(MeetingRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Create new meeting request
            var meeting = new Meeting
            {
                Title = model.Title,
                Description = model.Description,
                RequestedDateTime = model.RequestedDateTime,
                BrandUserId = currentUser.Id,
                Status = Constants.MeetingStatusPending,
                CreatedAt = DateTime.Now
            };

            _context.Add(meeting);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"New meeting request created by {currentUser.Email}");

            return RedirectToAction(nameof(MeetingStatus), new { id = meeting.Id });
        }

        public async Task<IActionResult> MeetingStatus(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == id && m.BrandUserId == currentUser.Id);

            if (meeting == null)
            {
                return NotFound();
            }

            return View(meeting);
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var pendingMeetings = await _context.Meetings
                .Where(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusPending)
                .CountAsync();

            var approvedMeetings = await _context.Meetings
                .Where(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusApproved)
                .CountAsync();

            var rejectedMeetings = await _context.Meetings
                .Where(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusRejected)
                .CountAsync();

            var upcomingMeetings = await _context.Meetings
                .Where(m => m.BrandUserId == currentUser.Id && 
                       m.Status == Constants.MeetingStatusApproved && 
                       m.ConfirmedDateTime > DateTime.Now)
                .OrderBy(m => m.ConfirmedDateTime)
                .ToListAsync();

            ViewBag.PendingMeetings = pendingMeetings;
            ViewBag.ApprovedMeetings = approvedMeetings;
            ViewBag.RejectedMeetings = rejectedMeetings;
            ViewBag.UpcomingMeetings = upcomingMeetings;

            return View();
        }
    }
}