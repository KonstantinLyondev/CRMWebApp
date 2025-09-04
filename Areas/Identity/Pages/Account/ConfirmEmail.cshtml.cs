using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace CRMWebApp.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _userManager = userManager;
            _cache = cache;
        }

        public string? ReturnUrl { get; private set; }

        private static string TokenKey(string rawCode)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawCode ?? string.Empty)));
            return $"emailconfirm:used:{hash}";
        }

        public async Task<IActionResult> OnGetAsync(string userId, string code, string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Page("/Index");

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
                return RedirectToPage("/Index");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"Unable to load user with ID '{userId}'.");

            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            if (_cache.TryGetValue(TokenKey(decoded), out _))
            {
                TempData["ErrorMessage"] = "This email confirmation link has already been used. Please sign in.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var result = await _userManager.ConfirmEmailAsync(user, decoded);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "An error occurred while confirming your email.";
                return RedirectToPage("/Index");
            }

            _cache.Set(
                TokenKey(decoded),
                true,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });

            TempData["SuccessMessage"] = "Your email was confirmed successfully. Please sign in.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}
