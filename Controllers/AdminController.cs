using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrandHandlerWebApp.Data;
using BrandHandlerWebApp.Helpers;
using BrandHandlerWebApp.Models;
using BrandHandlerWebApp.Services;
using BrandHandlerWebApp.ViewModels;

namespace BrandHandlerWebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            UserManager<Users> userManager,
            IEmailService emailService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> ReviewMeetings()
        {
            var pendingMeetings = await _context.Meetings
                .Where(m => m.Status == Constants.MeetingStatusPending)
                .Include(m => m.BrandUser)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(pendingMeetings);
        }

        public async Task<IActionResult> MeetingDetails(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
            {
                return NotFound();
            }

            return View(meeting);
        }


        
        public IActionResult ManageUsers()
        {
             return View();
        }


        [HttpGet]
        public async Task<IActionResult> ApproveMeeting(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
            {
                return NotFound();
            }

            var viewModel = new MeetingApprovalViewModel
            {
                MeetingId = meeting.Id,
                ConfirmedDateTime = (DateTime)meeting.RequestedDateTime
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMeeting(MeetingApprovalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == model.MeetingId);

            if (meeting == null)
            {
                return NotFound();
            }

            // Get current admin user
            var adminUser = await _userManager.GetUserAsync(User);

            // Update meeting details
            meeting.Status = Constants.MeetingStatusApproved;
            meeting.ConfirmedDateTime = model.ConfirmedDateTime;
            meeting.MeetingLink = model.MeetingLink;
            meeting.AdminUserId = adminUser.Id;
            meeting.UpdatedAt = DateTime.Now;

            _context.Update(meeting);
            await _context.SaveChangesAsync();

            // Send confirmation email to brand user
            await _emailService.SendMeetingConfirmationAsync(meeting);

            _logger.LogInformation($"Meeting {meeting.Id} approved by admin {adminUser.Email}");

            return RedirectToAction(nameof(ReviewMeetings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectMeeting(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
            {
                return NotFound();
            }

            // Get current admin user
            var adminUser = await _userManager.GetUserAsync(User);

            // Update meeting status
            meeting.Status = Constants.MeetingStatusRejected;
            meeting.AdminUserId = adminUser.Id;
            meeting.UpdatedAt = DateTime.Now;

            _context.Update(meeting);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Meeting {meeting.Id} rejected by admin {adminUser.Email}");

            return RedirectToAction(nameof(ReviewMeetings));
        }
    }
}