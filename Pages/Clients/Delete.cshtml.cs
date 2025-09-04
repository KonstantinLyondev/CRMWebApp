using System;
using System.Linq;
using System.Threading.Tasks;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Clients
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        public int DealsCount { get; private set; }
        public int InteractionsCount { get; private set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var c = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            Client = c;

            DealsCount = await _context.Deals
                .Where(d => d.ClientId == c.Id && !d.IsDeleted)
                .CountAsync();

            InteractionsCount = await _context.Interactions
                .Where(i => i.ClientId == c.Id && !i.IsDeleted)
                .CountAsync();

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients
                .FirstOrDefaultAsync(x => x.Id == id);

            if (client == null) return NotFound();

            var deals = await _context.Deals
                .Where(d => d.ClientId == client.Id && !d.IsDeleted)
                .ToListAsync();

            var interactions = await _context.Interactions
                .Where(i => i.ClientId == client.Id && !i.IsDeleted)
                .ToListAsync();

            foreach (var d in deals) d.IsDeleted = true;
            foreach (var it in interactions) it.IsDeleted = true;
            client.IsDeleted = true;

            var userId = _userManager.GetUserId(User); 
            var nowUtc = DateTime.UtcNow;

            foreach (var d in deals)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Delete",
                    DealId = d.Id,         
                    UserId = userId,
                    Timestamp = nowUtc
                });
            }

            foreach (var it in interactions)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Delete",
                    InteractionId = it.Id, 
                    UserId = userId,
                    Timestamp = nowUtc
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Delete",
                ClientId = client.Id,      
                UserId = userId,
                Timestamp = nowUtc
            });

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Client \"{client.Name}\" was deleted along with {deals.Count} deal(s) and {interactions.Count} interaction(s).";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "An error occurred while deleting the client.";
                return RedirectToPage("./Details", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}
