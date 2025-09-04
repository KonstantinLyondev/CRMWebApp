using System;
using System.Threading.Tasks;
using CRMWebApp.Authorization;
using CRMWebApp.Data;
using CRMWebApp.Models;
using CRMWebApp.Services; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Deals
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthorizationService _authorization;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserContext _ctx; 

        public DeleteModel(
            AppDbContext context,
            IAuthorizationService authorization,
            UserManager<ApplicationUser> userManager,
            IUserContext ctx)
        {
            _context = context;
            _authorization = authorization;
            _userManager = userManager;
            _ctx = ctx;
        }

        public Deal Deal { get; set; } = default!;
        public int InteractionsCount { get; set; }
        public bool CanDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var entity = await _context.Deals
                .Include(d => d.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (entity == null) return NotFound();

            var auth = await _authorization.AuthorizeAsync(User, entity, Operations.Delete);

            Deal = entity;
            CanDelete = auth.Succeeded;

            InteractionsCount = await _context.Interactions
                .CountAsync(i => i.DealId == id && !i.IsDeleted);

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int id)
        {
            var entity = await _context.Deals
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (entity == null) return NotFound();

            var auth = await _authorization.AuthorizeAsync(User, entity, Operations.Delete);
            if (!auth.Succeeded) return Forbid();

            entity.IsDeleted = true;

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Delete",
                DealId = entity.Id,                      
                UserId = _userManager.GetUserId(User),   
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Deal deleted successfully!";
            return RedirectToPage("./Index");
        }
    }
}
