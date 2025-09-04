using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using CRMWebApp.Helpers;  

namespace CRMWebApp.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public EmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        public string Email { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            [Display(Name = "New email")]
            public string? NewEmail { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email ?? string.Empty;

            Input = new InputModel { NewEmail = string.Empty };
            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var currentEmail = await _userManager.GetEmailAsync(user) ?? string.Empty;
            var newEmail = (Input.NewEmail ?? string.Empty).Trim();

            if (string.Equals(newEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input.NewEmail", "The new email must be different from the current email.");
                await LoadAsync(user);
                return Page();
            }

            var existingUser = await _userManager.FindByEmailAsync(newEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("Input.NewEmail", "This email address is already associated with another account.");
                await LoadAsync(user);
                return Page();
            }

            try
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                var codeEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    pageName: "/Account/Manage/ConfirmEmailChangeWithPassword",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = newEmail, code = codeEncoded },
                    protocol: Request.Scheme
                );

                var isImpersonating = User.IsImpersonating();
                var initiator = isImpersonating ? "an administrator" : "you";

                // Send to NEW email
                await _emailSender.SendEmailAsync(
                    newEmail,
                    "Confirm your email change",
                    $@"A request to change your account email to this address was initiated by {initiator}.
Please confirm the change by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.
If you did not request this, please ignore this email.");

                // Notify OLD email
                await _emailSender.SendEmailAsync(
                    currentEmail,
                    "Email change requested",
                    $@"A request to change your account email to <strong>{HtmlEncoder.Default.Encode(newEmail)}</strong> was initiated by {initiator}.
A confirmation email has been sent to the new address. No changes will apply unless confirmed with your password.");

                StatusMessage = "Confirmation link sent. Please check the new email address (including Spam/Promotions).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Failed to send email: {ex.Message}");
                Email = currentEmail;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user) ?? string.Empty;

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var codeEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                pageName: "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = codeEncoded },
                protocol: Request.Scheme
            );

            await _emailSender.SendEmailAsync(
                email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>."
            );

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToPage();
        }
    }
}