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
            // Get counts for dashboard stats
            var pendingMeetings = await _context.Meetings
                .CountAsync(m => m.Status == Constants.MeetingStatusPending);

            var approvedMeetings = await _context.Meetings
                .CountAsync(m => m.Status == Constants.MeetingStatusApproved);

            var rejectedMeetings = await _context.Meetings
                .CountAsync(m => m.Status == Constants.MeetingStatusRejected);

            var brandUsers = await _context.Users
                .CountAsync(u => u.Role == "Brand");

            // Get recent meetings (last 5)
            var recentMeetings = await _context.Meetings
                .Include(m => m.BrandUser)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Pass data to view
            ViewBag.PendingMeetings = pendingMeetings;
            ViewBag.ApprovedMeetings = approvedMeetings;
            ViewBag.RejectedMeetings = rejectedMeetings;
            ViewBag.BrandUsers = brandUsers;
            ViewBag.RecentMeetings = recentMeetings;

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
                _logger.LogWarning($"Meeting with ID {id} not found for approval.");
                return NotFound();
            }

            return View(meeting);
        }


        
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users
                .Where(u => u.Role == "Brand")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
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
                foreach (var modelStateEntry in ModelState.Values)
                {
                    foreach (var error in modelStateEntry.Errors)
                    {
                        _logger.LogError($"Model Error: {error.ErrorMessage}");
                    }
                }
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
            _logger.LogInformation($"Attempting to save changes for meeting {meeting.Id}.");
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Changes saved successfully for meeting {meeting.Id}.");

            // Send confirmation email to brand user with meeting details, limit to 3 notifications
            if (meeting.NotificationCount < 3)
            {
                var emailSubject = "Your Meeting Request Has Been Approved";
                var emailBody = $"Hello {meeting.BrandUser.FullName},\n\n" +
                              $"Your meeting request for '{meeting.Title}' has been approved.\n\n" +
                              $"Confirmed Date & Time: {meeting.ConfirmedDateTime?.ToString("f")}\n" +
                              $"Meeting Link: {meeting.MeetingLink}\n\n" +
                              "Please let us know if you have any questions.\n\n" +
                              "Best regards,\n" +
                              "The Admin Team";
                              
                await _emailService.SendEmailAsync(meeting.BrandUser.Email, emailSubject, emailBody);
                meeting.NotificationCount++;
                _context.Update(meeting);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Meeting approved and notification email sent.";
            }
            else
            {
                TempData["InfoMessage"] = "Meeting approved, but notification limit reached. Email not sent.";
            }

            _logger.LogInformation($"Meeting {meeting.Id} approved by admin {adminUser.Email}.");
            _logger.LogInformation($"Redirecting to ReviewMeetings after approving meeting {meeting.Id}.");
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