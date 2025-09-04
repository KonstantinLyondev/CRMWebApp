// Areas/Identity/Pages/Account/Manage/ConfirmEmailChangeWithPassword.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace CRMWebApp.Areas.Identity.Pages.Account.Manage
{
    public class ConfirmEmailChangeWithPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMemoryCache _cache;

        public ConfirmEmailChangeWithPasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cache = cache;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required] public string UserId { get; set; } = string.Empty;
            [Required, EmailAddress] public string Email { get; set; } = string.Empty;
            [Required] public string Code { get; set; } = string.Empty; // base64url encoded

            [Required, DataType(DataType.Password)]
            [Display(Name = "Your password")]
            public string Password { get; set; } = string.Empty;
        }

        private bool IsImpersonating(ClaimsPrincipal user)
            => user.Identity?.IsAuthenticated == true && user.HasClaim("impersonating", "true");

        private static string TokenKey(string decodedCode)
        {
            using var sha = SHA256.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(decodedCode ?? string.Empty)));
            return $"emailchange:used:{hash}";
        }

        public IActionResult OnGet(string userId, string email, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                TempData["ErrorMessage"] = "Invalid email change link.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            if (IsImpersonating(User))
                return Forbid();

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch
            {
                TempData["ErrorMessage"] = "Invalid email change link.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            if (_cache.TryGetValue(TokenKey(decoded), out _))
            {
                TempData["ErrorMessage"] = "This email change link has already been used. Please sign in.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            TempData.Remove("StatusMessage");
            Input.UserId = userId;
            Input.Email = email;
            Input.Code = code;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (IsImpersonating(User))
                return Forbid();

            var currentUser = await _userManager.FindByIdAsync(Input.UserId);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "Invalid email change link.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            string decodedCode;
            try
            {
                decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired link.");
                return Page();
            }

            if (_cache.TryGetValue(TokenKey(decodedCode), out _))
            {
                TempData["ErrorMessage"] = "This email change link has already been used. Please sign in.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var passwordOk = await _userManager.CheckPasswordAsync(currentUser, Input.Password);
            if (!passwordOk)
            {
                ModelState.AddModelError("Input.Password", "Incorrect password.");
                return Page();
            }

            var result = await _userManager.ChangeEmailAsync(currentUser, Input.Email, decodedCode);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

            var currentUserName = await _userManager.GetUserNameAsync(currentUser);
            if (!string.Equals(currentUserName, Input.Email, StringComparison.OrdinalIgnoreCase))
                await _userManager.SetUserNameAsync(currentUser, Input.Email);

            _cache.Set(TokenKey(decodedCode), true,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                });

            await _signInManager.RefreshSignInAsync(currentUser);

            TempData["SuccessMessage"] = "Your email has been changed successfully.";
            return RedirectToPage("/Account/Manage/Email", new { area = "Identity" });
        }
    }
}
