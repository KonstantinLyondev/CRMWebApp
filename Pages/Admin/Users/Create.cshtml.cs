using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;

namespace CRMWebApp.Pages.Admin.Users
{
    [Authorize(Policy = "AdminWithMfa")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateModel(UserManager<ApplicationUser> userManager,
                           IEmailSender emailSender,
                           RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Full name")]
            public string? FullName { get; set; }

            [Display(Name = "Make Admin")]
            public bool MakeAdmin { get; set; } = false;

            [Display(Name = "Send password setup link")]
            public bool SendPasswordSetup { get; set; } = true;
        }

        [TempData] public string? Success { get; set; }
        [TempData] public string? Error { get; set; }

        public void OnGet() { }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var existing = await _userManager.FindByEmailAsync(Input.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(Input.Email), "Email is already in use.");
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                IsDeactivated = false
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            if (Input.MakeAdmin)
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                await _userManager.AddToRoleAsync(user, "User");
            }

            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedConfirm = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmToken));
            var confirmUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = encodedConfirm },
                protocol: Request.Scheme)!;

            var sb = new StringBuilder();
            sb.AppendLine($"Hello{(string.IsNullOrWhiteSpace(user.FullName) ? "" : " " + HtmlEncoder.Default.Encode(user.FullName))},");
            sb.AppendLine("<br/><br/>An account has been created for you in the CRM system.");
            sb.AppendLine("<br/>Please confirm your email to activate your account:");
            sb.AppendLine($"<br/><a href=\"{HtmlEncoder.Default.Encode(confirmUrl)}\">Confirm your email</a>");

            if (Input.SendPasswordSetup)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedReset = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
                var resetUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code = encodedReset },
                    protocol: Request.Scheme)!;

                sb.AppendLine("<br/><br/>After confirming your email, set your password here:");
                sb.AppendLine($"<br/><a href=\"{HtmlEncoder.Default.Encode(resetUrl)}\">Set your password</a>");
            }

            await _emailSender.SendEmailAsync(user.Email!, "Confirm your account", sb.ToString());

            Success = $"User {user.Email} was created. A confirmation email has been sent.";
            return RedirectToPage("./Index");
        }
    }
}
