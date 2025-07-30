using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BrandHandlerWebApp.Data;
using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Controllers
{
    [Authorize]
    public class MeetingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public MeetingController(
            AppDbContext context,
            UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var meeting = await _context.Meetings
                .Include(m => m.BrandUser)
                .Include(m => m.AdminUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meeting == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this meeting
            if (!isAdmin && meeting.BrandUserId != currentUser.Id)
            {
                return Forbid();
            }

            return View(meeting);
        }

        [Authorize(Roles = "Brand")]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var meeting = await _context.Meetings
                .FirstOrDefaultAsync(m => m.Id == id && m.BrandUserId == currentUser.Id);

            if (meeting == null)
            {
                return NotFound();
            }

            // Only allow cancellation of pending meetings
            if (meeting.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending meetings can be cancelled.";
                return RedirectToAction("Details", new { id = meeting.Id });
            }

            _context.Meetings.Remove(meeting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Meeting request cancelled successfully.";
            return RedirectToAction("Index", "Brand");
        }
    }
}