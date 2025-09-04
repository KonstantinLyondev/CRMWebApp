using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IUserContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDealScoringService _scoring; 

        public CreateModel(AppDbContext context,
                           IUserContext ctx,
                           UserManager<ApplicationUser> userManager,
                           IDealScoringService scoring)   
        {
            _context = context;
            _ctx = ctx;
            _userManager = userManager;
            _scoring = scoring;
        }

        [BindProperty]
        public Deal Deal { get; set; } = new Deal();

        public SelectList ClientsList { get; set; } = default!;

        public void OnGet()
        {
            var effectiveUserId = _ctx.UserId!;

            var clientsQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

            ClientsList = new SelectList(
                clientsQ.OrderBy(c => c.Name),
                nameof(Client.Id), nameof(Client.Name));
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var effectiveUserId = _ctx.UserId!;

            var clientsQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

            ClientsList = new SelectList(
                clientsQ.OrderBy(c => c.Name),
                nameof(Client.Id), nameof(Client.Name), Deal.ClientId);

            Deal.UserId = effectiveUserId;
            Deal.CreatedById = effectiveUserId;

            ModelState.Remove("Deal.UserId");
            ModelState.Remove("Deal.CreatedById");

            if (!ModelState.IsValid) return Page();

            var clientOk = await clientsQ.AnyAsync(c => c.Id == Deal.ClientId);
            if (!clientOk)
            {
                ModelState.AddModelError("Deal.ClientId", "Please select a valid client.");
                return Page();
            }

            Deal.IsDeleted = false;
            Deal.CreatedAt = DateTime.UtcNow;
            Deal.UpdatedAt = null;

            Deal.Probability = await _scoring.ComputeProbabilityAsync(Deal);

            _context.Deals.Add(Deal);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Create",
                DealId = Deal.Id,
                UserId = _userManager.GetUserId(User),
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Deal created successfully!";
            return RedirectToPage("./Index");
        }
    }
}
