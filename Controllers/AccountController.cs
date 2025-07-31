using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BrandHandlerWebApp.Models;
using BrandHandlerWebApp.ViewModels;
using BrandHandlerWebApp.Services;
using BrandHandlerWebApp.Helpers;
using System.Text.Encodings.Web;

namespace BrandHandlerWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<Users> signInManager, 
            UserManager<Users> userManager, 
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if the user exists
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "You must confirm your email before logging in.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                
                // Redirect based on role
                if (await _userManager.IsInRoleAsync(user, Constants.AdminRole))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (await _userManager.IsInRoleAsync(user, Constants.BrandRole))
                {
                    return RedirectToAction("Index", "Brand");
                }
                
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            // Validate organization email
            if (!model.Email.Contains("@") || model.Email.Split('@')[1].ToLower() == "gmail.com" || 
                model.Email.Split('@')[1].ToLower() == "yahoo.com" || model.Email.Split('@')[1].ToLower() == "hotmail.com")
            {
                ModelState.AddModelError("Email", "Please use your organization email address.Dont use .hotmail,.gmail,.yahoo");
                return View(model);
            }
            
            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                Email = model.Email,
                Role = Constants.BrandRole,
                EmailConfirmed = false
            };
            
            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                
                // Add user to Brand role
                await _userManager.AddToRoleAsync(user, Constants.BrandRole);
                
                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Save token to user
                user.ConfirmationToken = token;
                await _userManager.UpdateAsync(user);
                
                // Create confirmation link
                var confirmationLink = Url.Action("ConfirmEmail", "Account", 
                    new { userId = user.Id, token = token }, Request.Scheme);
                
                // Send confirmation email
                await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);
                
                return RedirectToAction("EmailConfirmationSent");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            return View(model);
        }
        [HttpGet]
        public IActionResult EmailConfirmationSent()
        {
            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }
            
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Error confirming email for user with ID '{userId}':");
            }
            
            // Update user's EmailConfirmed status
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            
            return View();
        }
        
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }
        
        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }
            else
            {
                return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
            }
        }

        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }

            return View(new ChangePasswordViewModel { Email = username });
        }


        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }

            var result = await _userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                return RedirectToAction("Login", "Account",result);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }
        }


       

    }
}
