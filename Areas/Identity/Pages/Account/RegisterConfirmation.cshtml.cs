#nullable disable
using System;
using System.Text;
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
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;

        public RegisterConfirmationModel(
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

        public bool AlreadyConfirmed { get; private set; }
        public int CooldownSeconds { get; private set; }

        private string CacheKey(string email) => $"emailconfirm:cooldown:{email?.Trim().ToLower()}";

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToPage("/Index");

            Email = email;

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
                return NotFound($"Unable to load user with email '{Email}'.");

            AlreadyConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            CooldownSeconds = GetRemainingSeconds(Email);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
                return await OnGetAsync(null);

            var remaining = GetRemainingSeconds(Email);
            if (remaining > 0)
            {
                CooldownSeconds = remaining;
                TempData["Message"] = $"Please wait {remaining}s before requesting a new confirmation email.";
                return await OnGetAsync(Email);
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
                return await OnGetAsync(Email);

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                AlreadyConfirmed = true;
                return await OnGetAsync(Email);
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code = encoded },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            _cache.Set(CacheKey(Email), DateTimeOffset.UtcNow.AddSeconds(CooldownSecondsDefault),
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CooldownSecondsDefault) });

            CooldownSeconds = CooldownSecondsDefault;
            TempData["Message"] = "A new confirmation email has been sent.";
            return await OnGetAsync(Email);
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
