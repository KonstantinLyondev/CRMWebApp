using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRMWebApp.Models;
using CRMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Admin.Users
{
    [Authorize(Policy = "AdminWithMfa")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserLifecycleService _lifecycle;

        public IndexModel(UserManager<ApplicationUser> userManager,
                          SignInManager<ApplicationUser> signInManager,
                          IUserLifecycleService lifecycle)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _lifecycle = lifecycle;
        }

        public List<UserRow> Users { get; private set; } = new();

        [TempData] public string? Success { get; set; }
        [TempData] public string? Error { get; set; }

        public class UserRow
        {
            public string Id { get; set; } = default!;
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string Roles { get; set; } = "";
            public bool EmailConfirmed { get; set; }
            public DateTimeOffset? LockoutEnd { get; set; }
            public bool IsDeactivated { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsSelf { get; set; }
            public bool IsSystemAccount { get; set; }
        }

        public async Task OnGetAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            var myId = me?.Id;

            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync();

            Users = await BuildRowsAsync(users, myId);
        }

        public async Task<JsonResult> OnGetAutocompleteAsync(string? term)
        {
            term = (term ?? string.Empty).Trim();

            var emails = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Email != null && EF.Functions.Like(u.Email!, term + "%"))
                .OrderBy(u => u.Email)
                .Select(u => u.Email!)
                .Distinct()
                .Take(8)
                .ToListAsync();

            return new JsonResult(emails);
        }

        public async Task<PartialViewResult> OnGetFilterAsync(string? query)
        {
            query = (query ?? string.Empty).Trim();

            var me = await _userManager.GetUserAsync(User);
            var myId = me?.Id;

            var q = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(u => u.Email != null && EF.Functions.Like(u.Email!, query + "%"));
            }

            var users = await q.OrderBy(u => u.Email).ToListAsync();
            var rows = await BuildRowsAsync(users, myId);

            return Partial("_UserRows", rows);
        }

        private async Task<List<UserRow>> BuildRowsAsync(IList<ApplicationUser> users, string? myId)
        {
            var list = new List<UserRow>(users.Count);
            foreach (var u in users)
            {
                if (string.Equals(u.Email, "admin@crm.local", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Email, "system@crm.local", StringComparison.OrdinalIgnoreCase))
                    continue;

                var roles = await _userManager.GetRolesAsync(u);
                var isAdmin = roles.Contains("Admin");

                list.Add(new UserRow
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Roles = string.Join(", ", roles),
                    EmailConfirmed = u.EmailConfirmed,
                    LockoutEnd = u.LockoutEnd,
                    IsDeactivated = u.IsDeactivated,
                    IsAdmin = isAdmin,
                    IsSelf = u.Id == myId,
                    IsSystemAccount = false
                });
            }
            return list;
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostMakeAdminAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToPage();

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();
            if (me.Id == id) { Error = "You cannot change your own admin role here."; return RedirectToPage(); }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { Error = "User not found."; return RedirectToPage(); }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                Success = $"User {user.Email} is already Admin.";
                return RedirectToPage();
            }

            var res = await _userManager.AddToRoleAsync(user, "Admin");
            if (!res.Succeeded)
            {
                Error = string.Join("; ", res.Errors.Select(e => e.Description));
                return RedirectToPage();
            }

            await _userManager.UpdateSecurityStampAsync(user);

            Success = $"User {user.Email} was promoted to Admin.";
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAdminAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToPage();

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();
            if (me.Id == id) { Error = "You cannot change your own admin role here."; return RedirectToPage(); }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { Error = "User not found."; return RedirectToPage(); }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var rm = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!rm.Succeeded)
                {
                    Error = string.Join("; ", rm.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }
            }

            var disable2fa = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2fa.Succeeded)
            {
                Error = "Admin role removed, but failed to disable 2FA.";
                return RedirectToPage();
            }

            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
            await _userManager.UpdateSecurityStampAsync(user);

            Success = $"Admin role removed and 2FA disabled for {user.Email}.";
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostImpersonateAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToPage();

            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return Forbid();

            var target = await _userManager.FindByIdAsync(id);
            if (target == null || target.Id == admin.Id) return RedirectToPage();

            if (await _userManager.IsInRoleAsync(target, "Admin")) return RedirectToPage();

            var adminHadMfa = User.HasClaim("amr", "mfa");
            var extraClaims = new List<Claim>
            {
                new("Impersonating", "true"),
                new("ImpersonatorId", admin.Id),
                new("ImpersonatorHadMfa", adminHadMfa ? "true" : "false")
            };

            await _signInManager.SignOutAsync();
            await _signInManager.SignInWithClaimsAsync(target, isPersistent: false, extraClaims);

            return RedirectToPage("/Index");
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeactivateAsync(string id)
        {
            var me = await _userManager.GetUserAsync(User);
            var (ok, error) = await _lifecycle.DeactivateAsync(id, "Deactivated by admin", me!.Id);
            if (ok) Success = "User has been deactivated.";
            else Error = error;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostReactivateAsync(string id)
        {
            var me = await _userManager.GetUserAsync(User);
            var (ok, error) = await _lifecycle.ReactivateAsync(id, me!.Id);
            if (ok) Success = "User has been reactivated.";
            else Error = error;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToPage();

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();
            if (me.Id == id) { Error = "You cannot delete your own account."; return RedirectToPage(); }

            var (ok, err) = await _lifecycle.HardDeleteAsync(id, me.Id);
            if (!ok) { Error = err; return RedirectToPage(); }

            Success = "User was permanently deleted.";
            return RedirectToPage();
        }

    }
}