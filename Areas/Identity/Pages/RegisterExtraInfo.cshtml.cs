using System.Threading.Tasks;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CRMWebApp.Areas.Identity.Pages
{
    [AllowAnonymous]
    public class RegisterExtraInfoModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterExtraInfoModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty] public string UserId { get; set; }

                [BindProperty(SupportsGet = false)]
        public string Email { get; set; }

        [BindProperty] public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [StringLength(100)] public string? FullName { get; set; }
            [StringLength(100)] public string? Company { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Index", new { area = "" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToPage("/Index", new { area = "" });

            UserId = userId;
            Email = user.Email;
            Input.FullName = user.FullName;
            Input.Company = user.Company;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (string.IsNullOrWhiteSpace(UserId))
                return RedirectToPage("/Index", new { area = "" });

            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
                return RedirectToPage("/Index", new { area = "" });

            user.FullName = Input.FullName?.Trim();
            user.Company = Input.Company?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return Page();
            }

                        return RedirectToPage("/Account/RegisterConfirmation", new { area = "Identity", email = user.Email });
        }
    }
}
