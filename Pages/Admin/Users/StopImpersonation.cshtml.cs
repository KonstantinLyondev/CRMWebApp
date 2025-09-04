using System.Security.Claims;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMWebApp.Pages.Admin.Users
{
        public class StopImpersonationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public StopImpersonationModel(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
        {
            _users = users;
            _signIn = signIn;
        }

        public async Task<IActionResult> OnPostAsync()
        {
                        var adminId = User.FindFirstValue("ImpersonatorId");
            if (string.IsNullOrEmpty(adminId))
                return RedirectToPage("/Index");

            var admin = await _users.FindByIdAsync(adminId);
            if (admin == null)
                return RedirectToPage("/Index");

                        var adminHadMfa = User.HasClaim("ImpersonatorHadMfa", "true");

            await _signIn.SignOutAsync();

            if (adminHadMfa)
            {
                var extra = new List<Claim> { new("amr", "mfa") };
                await _signIn.SignInWithClaimsAsync(admin, isPersistent: false, extra);
            }
            else
            {
                await _signIn.SignInAsync(admin, isPersistent: false);
            }

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}
