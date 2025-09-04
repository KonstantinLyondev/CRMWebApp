using System;
using System.Threading.Tasks;
using CRMWebApp.Authorization;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Interactions
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthorizationService _authorization;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(AppDbContext context,
                           IAuthorizationService authorization,
                           UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _authorization = authorization;
            _userManager = userManager;
        }

        public Interaction Interaction { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var entity = await _context.Interactions
                .Include(i => i.Client)
                .Include(i => i.Deal)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (entity == null) return NotFound();

            var canDelete = await _authorization.AuthorizeAsync(User, entity, Operations.Delete);
            if (!canDelete.Succeeded) return Forbid();

            Interaction = entity;
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int id)
        {
            var entity = await _context.Interactions
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (entity == null) return NotFound();

            var canDelete = await _authorization.AuthorizeAsync(User, entity, Operations.Delete);
            if (!canDelete.Succeeded) return Forbid();

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Delete",
                InteractionId = entity.Id,                 
                UserId = _userManager.GetUserId(User),     
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Interaction deleted successfully!";
            return RedirectToPage("./Index");
        }
    }
}
