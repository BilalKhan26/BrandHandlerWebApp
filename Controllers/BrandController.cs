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

        public BrandController(AppDbContext context, UserManager<Users> userManager, ILogger<BrandController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Brand/EditMeeting/5
        public async Task<IActionResult> EditMeeting(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null)
            {
                return NotFound();
            }

            // Check if the meeting belongs to the current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (meeting.BrandUserId != currentUser.Id)
            {
                return Forbid();
            }

            // Only allow editing if meeting is still pending
            if (meeting.Status != Constants.MeetingStatusPending)
            {
                TempData["ErrorMessage"] = "Cannot edit meetings that are not pending.";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(meeting);
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
            TempData["SuccessMessage"] = "Meeting request submitted successfully!";

            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Brand/EditMeeting/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeeting(int id, Meeting meeting)
        {
            // Check if the meeting belongs to the current user
            var currentUser = await _userManager.GetUserAsync(User);
            var existingMeeting = await _context.Meetings.FindAsync(id);
            if (existingMeeting == null || existingMeeting.BrandUserId != currentUser.Id)
            {
                return Forbid();
            }

            // Only allow editing if meeting is still pending
            if (existingMeeting.Status != Constants.MeetingStatusPending)
            {
                TempData["ErrorMessage"] = "Cannot edit meetings that are not pending.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingMeeting.Title = meeting.Title;
                    existingMeeting.Description = meeting.Description;
                    existingMeeting.RequestedDateTime = meeting.RequestedDateTime;
                    existingMeeting.UpdatedAt = DateTime.Now;
                    _context.Update(existingMeeting);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Meeting updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeetingExists(meeting.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Dashboard));
            }
            return View(meeting);
        }

        private bool MeetingExists(int id)
        {
            return _context.Meetings.Any(e => e.Id == id);
        }

        // POST: Brand/DeleteMeeting/5
        [HttpPost, ActionName("DeleteMeeting")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeetingConfirmed(int id)
        {
            var meeting = await _context.Meetings.FindAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (meeting == null || meeting.BrandUserId != currentUser.Id)
            {
                return NotFound();
            }

            // Only allow deletion if meeting is still pending
            if (meeting.Status != Constants.MeetingStatusPending)
            {
                TempData["ErrorMessage"] = "Cannot delete meetings that are not pending.";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Meeting deleted successfully!";
            return RedirectToAction(nameof(Dashboard));
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

            // Get counts for dashboard stats
            var pendingMeetings = await _context.Meetings
                .CountAsync(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusPending);

            var approvedMeetings = await _context.Meetings
                .CountAsync(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusApproved);

            var rejectedMeetings = await _context.Meetings
                .CountAsync(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusRejected);

            // Get upcoming meetings (approved meetings with future dates)
            var upcomingMeetings = await _context.Meetings
                .Where(m => m.BrandUserId == currentUser.Id && m.Status == Constants.MeetingStatusApproved && m.ConfirmedDateTime > DateTime.Now)
                .OrderBy(m => m.ConfirmedDateTime)
                .ToListAsync();

            // Create view model
            var viewModel = new BrandDashboardViewModel
            {
                PendingMeetings = pendingMeetings,
                ApprovedMeetings = approvedMeetings,
                RejectedMeetings = rejectedMeetings,
                UpcomingMeetings = upcomingMeetings
            };

            return View(viewModel);
        }
    }
}