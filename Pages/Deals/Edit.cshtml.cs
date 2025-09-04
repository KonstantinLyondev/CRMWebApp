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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Deals
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthorizationService _authorization;
        private readonly IUserContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDealScoringService _scoring;  

        public EditModel(AppDbContext context,
                         IAuthorizationService authorization,
                         IUserContext ctx,
                         UserManager<ApplicationUser> userManager,
                         IDealScoringService scoring)   
        {
            _context = context;
            _authorization = authorization;
            _ctx = ctx;
            _userManager = userManager;
            _scoring = scoring;
        }

        [BindProperty]
        public Deal Deal { get; set; } = default!;

        public SelectList ClientsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var entity = await _context.Deals
                .Include(d => d.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (entity == null) return NotFound();

            var canEdit = await _authorization.AuthorizeAsync(User, entity, Operations.Edit);
            if (!canEdit.Succeeded) return Forbid();

            Deal = entity;

            var clientsQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == entity.UserId);

            ClientsList = new SelectList(await clientsQ.ToListAsync(),
                                         nameof(Client.Id), nameof(Client.Name), Deal.ClientId);

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Deal.UserId");

            if (!ModelState.IsValid) return Page();

            var entity = await _context.Deals
                .FirstOrDefaultAsync(d => d.Id == Deal.Id && !d.IsDeleted);
            if (entity == null) return NotFound();

            var canEdit = await _authorization.AuthorizeAsync(User, entity, Operations.Edit);
            if (!canEdit.Succeeded) return Forbid();

            var clientOk = await _context.Clients
                .AnyAsync(c => c.Id == Deal.ClientId && !c.IsDeleted && c.UserId == entity.UserId);
            if (!clientOk)
            {
                ModelState.AddModelError("Deal.ClientId", "Please select a valid client.");
                return Page();
            }

            entity.Title = Deal.Title?.Trim() ?? string.Empty;
            entity.ClientId = Deal.ClientId;
            entity.Status = Deal.Status;
            entity.Amount = Deal.Amount;
            entity.Currency = Deal.Currency;
            entity.CloseDate = Deal.CloseDate;
            entity.Notes = Deal.Notes?.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            entity.Probability = await _scoring.ComputeProbabilityAsync(entity);

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Update",
                DealId = entity.Id,
                UserId = _userManager.GetUserId(User),
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Deal updated successfully!";
            return RedirectToPage("./Index");
        }
    }
}

