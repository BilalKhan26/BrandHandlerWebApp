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
            var pendingMeetings = await _context.Meetings.CountAsync(m => m.Status == Constants.MeetingStatusPending);
            var approvedMeetings = await _context.Meetings.CountAsync(m => m.Status == Constants.MeetingStatusApproved);
            var rejectedMeetings = await _context.Meetings.CountAsync(m => m.Status == Constants.MeetingStatusRejected);
            var brandUsers = await _context.Users.CountAsync(u => u.Role == "Brand");

            var recentMeetings = await _context.Meetings
                .Include(m => m.BrandUser)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.PendingMeetings = pendingMeetings;
            ViewBag.ApprovedMeetings = approvedMeetings;
            ViewBag.RejectedMeetings = rejectedMeetings;
            ViewBag.BrandUsers = brandUsers;
            ViewBag.RecentMeetings = recentMeetings;

            return View();
        }

        public async Task<IActionResult> ReviewMeetings()
        {
            var allMeetings = await _context.Meetings
                .Include(m => m.BrandUser)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(allMeetings);
        }

        public async Task<IActionResult> MeetingDetails(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
            {
                _logger.LogWarning($"Meeting with ID {id} not found for details.");
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

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return Conflict(new { message = "User with this email already exists." });
            }

            var user = new Users
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true, // Assuming email is confirmed upon admin creation
                Role = Constants.BrandRole
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Constants.BrandRole);
                // Send confirmation email
                var emailSubject = "Your BrandHandler account has been created";
                var emailBody = $"Hello {user.FullName},\n\nYour account has been created successfully.\nEmail: {user.Email}\n\nPlease log in to your account.\n\nBest regards,\nThe Admin Team";
                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);
                return Ok(new { message = "User added successfully and confirmation email sent." });
            }

            return BadRequest(new { message = "Failed to add user.", errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPut]
        public async Task<IActionResult> EditUser([FromBody] EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email; // Update UserName as well if email is changed

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "User updated successfully." });
            }

            return BadRequest(new { message = "Failed to update user.", errors = result.Errors.Select(e => e.Description) });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "User deleted successfully." });
            }

            return BadRequest(new { message = "Failed to delete user.", errors = result.Errors.Select(e => e.Description) });
        }

        [HttpGet]
        public async Task<IActionResult> ApproveMeeting(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null) return NotFound();

            var viewModel = new MeetingApprovalViewModel
            {
                MeetingId = meeting.Id,
                ConfirmedDateTime = meeting.RequestedDateTime ?? DateTime.Now
            };

            return View("ApproveMeeting", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMeeting(MeetingApprovalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for approval.");
                return View(model);
            }

            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == model.MeetingId);

            if (meeting == null) return NotFound();

            var adminUser = await _userManager.GetUserAsync(User);

            meeting.Status = Constants.MeetingStatusApproved;
            meeting.ConfirmedDateTime = model.ConfirmedDateTime;
            meeting.MeetingLink = model.MeetingLink;
            meeting.AdminUserId = adminUser.Id;
            meeting.UpdatedAt = DateTime.Now;
            meeting.AdminNotes = model.Notes;

            _context.Update(meeting);
            await _context.SaveChangesAsync();

            if (meeting.NotificationCount < 3)
              {
                  var emailSubject = "Your Meeting Request Has Been Approved";
                  var emailBody = $"Hello {meeting.BrandUser.FullName},\n\n" +
                                  $"Your meeting for '{meeting.Title}' has been approved.\n" +
                                  $"Confirmed Date & Time: {meeting.ConfirmedDateTime:f}\n" +
                                  $"Meeting Link: {meeting.MeetingLink}\n\n" +
                                  "Best regards,\nThe Admin Team";
  
                  await _emailService.SendEmailAsync(meeting.BrandUser.Email, emailSubject, emailBody);
                  meeting.NotificationCount++;
                  _context.Update(meeting);
                  await _context.SaveChangesAsync();
  
                  TempData["SuccessMessage"] = "Meeting approved and notification email sent.";
              }
              else
              {
                  TempData["InfoMessage"] = "Meeting approved, but email not sent (limit reached).";
              }
  
              return RedirectToAction(nameof(ReviewMeetings));
         }

        [HttpGet]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null) return NotFound();

            return View(meeting);
        }

        [ValidateAntiForgeryToken]
        [HttpPost, ActionName("DeleteMeetingConfirmed")]
        public async Task<IActionResult> DeleteMeetingConfirmed(int id)

        {
            var meeting = await _context.Meetings.FindAsync(id);
            if (meeting == null) return NotFound();

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Meeting deleted successfully.";
            return RedirectToAction(nameof(ReviewMeetings));
        }

        [HttpGet]
        public async Task<IActionResult> RejectMeeting(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null) return NotFound();

            var model = new MeetingRejectionViewModel
            {
                MeetingId = meeting.Id
            };

            return View("RejectMeeting", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectMeeting(MeetingRejectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("RejectMeeting", model);
            }

            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .FirstOrDefaultAsync(m => m.Id == model.MeetingId);

            if (meeting == null) return NotFound();

            var adminUser = await _userManager.GetUserAsync(User);

            meeting.Status = Constants.MeetingStatusRejected;
            meeting.AdminUserId = adminUser.Id;
            meeting.UpdatedAt = DateTime.Now;
            meeting.AdminNotes = model.RejectionReason;

            _context.Update(meeting);
            await _context.SaveChangesAsync();

            if (meeting.NotificationCount < 3)
            {
                var emailSubject = "Your Meeting Request Has Been Rejected";
                var emailBody = $"Hello {meeting.BrandUser.FullName},\n\n" +
                                $"Your meeting for '{meeting.Title}' has been rejected.\n" +
                                $"Reason: {model.RejectionReason}\n\n" +
                                "Best regards,\nThe Admin Team";

                await _emailService.SendEmailAsync(meeting.BrandUser.Email, emailSubject, emailBody);
                meeting.NotificationCount++;
                _context.Update(meeting);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Meeting rejected and email sent.";
            }
            else
            {
                TempData["InfoMessage"] = "Meeting rejected, but email not sent (limit reached).";
            }

            return RedirectToAction(nameof(ReviewMeetings));
        }


    }
}
