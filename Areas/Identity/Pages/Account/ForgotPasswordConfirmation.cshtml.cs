#nullable disable
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
using Microsoft.Extensions.Caching.Memory;   
namespace CRMWebApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;   
        public ForgotPasswordConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _cache = cache;
        }

        private const int CooldownSecondsDefault = 60;

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; }

                public int CooldownSeconds { get; private set; }

        private string CacheKey(string email) => $"pwdreset:cooldown:{email?.Trim().ToLower()}";

        public void OnGet()
        {
            CooldownSeconds = GetRemainingSeconds(Email);
        }

        public async Task<IActionResult> OnPostAsync()
        {
                        var remaining = GetRemainingSeconds(Email);
            if (remaining > 0)
            {
                CooldownSeconds = remaining;
                TempData["Message"] = $"Please wait {remaining}s before requesting a new link.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Email))
                return Page();

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                                return Page();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = encoded },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Email,
                "Reset Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        _cache.Set(CacheKey(Email), DateTimeOffset.UtcNow.AddSeconds(CooldownSecondsDefault),
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CooldownSecondsDefault) });

            CooldownSeconds = CooldownSecondsDefault;
            TempData["Message"] = "A new reset link has been sent to your email.";
            return Page();
        }

        private int GetRemainingSeconds(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return 0;
            if (_cache.TryGetValue(CacheKey(email), out DateTimeOffset nextAllowed))
            {
                var remaining = (int)Math.Ceiling((nextAllowed - DateTimeOffset.UtcNow).TotalSeconds);
                return remaining > 0 ? remaining : 0;
            }
            return 0;
        }
    }
}
