using System.Threading.Tasks;
using CRMWebApp.Data;
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
    public class DeleteModel : PageModel
    {
        private readonly IUserLifecycleService _lifecycle;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;

        public DeleteModel(
            IUserLifecycleService lifecycle,
            UserManager<ApplicationUser> userManager,
            AppDbContext db)
        {
            _lifecycle = lifecycle;
            _userManager = userManager;
            _db = db;
        }

        [BindProperty] public string Id { get; set; } = default!;
        public ApplicationUser? TargetUser { get; set; }

        public int ClientsCount { get; set; }
        public int DealsCount { get; set; }
        public int InteractionsCount { get; set; }
        public bool IsLastAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            TargetUser = u;
            Id = id;

            ClientsCount = await _db.Clients.CountAsync(x => x.UserId == id);
            DealsCount = await _db.Deals.CountAsync(x => x.UserId == id);
            InteractionsCount = await _db.Interactions.CountAsync(x => x.UserId == id);

            if (await _userManager.IsInRoleAsync(u, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                IsLastAdmin = admins.Count <= 1;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            var u = await _userManager.FindByIdAsync(Id);
            if (u == null) return NotFound();

            var (ok, error) = await _lifecycle.HardDeleteAsync(Id, me!.Id);
            TempData[ok ? "Success" : "Error"] = ok
                ? $"User '{u.Email}' has been permanently deleted."
                : error ?? "Delete failed.";

            return RedirectToPage("./Index");
        }
    }
}
