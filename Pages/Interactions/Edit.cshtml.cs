using System;
using System.Linq;
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

namespace CRMWebApp.Pages.Interactions
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IAuthorizationService _authorization;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserContext _ctx;
        public EditModel(AppDbContext context,
                         IAuthorizationService authorization,
                         UserManager<ApplicationUser> userManager,
                         IUserContext ctx)
        {
            _context = context;
            _authorization = authorization;
            _userManager = userManager;
            _ctx = ctx;
        }

        [BindProperty] public int Id { get; set; }
        [BindProperty] public int ClientId { get; set; }
        [BindProperty] public int? DealId { get; set; }
        [BindProperty] public DateTime Date { get; set; }
        [BindProperty] public string Type { get; set; } = string.Empty;
        [BindProperty] public string? Comment { get; set; }

        public SelectList ClientsList { get; set; } = default!;
        public SelectList TypeOptions { get; set; } = default!;

        private static readonly string[] AllowedTypes = new[]
        {
            "Phone Call", "Email", "Meeting", "Message", "Demo", "Follow-up", "Other"
        };

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var entity = await _context.Interactions
                .Include(i => i.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (entity == null) return NotFound();

            var canEdit = await _authorization.AuthorizeAsync(User, entity, Operations.Edit);
            if (!canEdit.Succeeded) return Forbid();

            Id = entity.Id;
            ClientId = entity.ClientId;
            DealId = entity.DealId;
            Date = entity.Date == default ? DateTime.Now : entity.Date;
            Type = entity.Type ?? string.Empty;
            Comment = entity.Comment;

            var effectiveUserId = _ctx.UserId!;
            var clientsQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

            ClientsList = new SelectList(await clientsQ.OrderBy(c => c.Name).ToListAsync(),
                                         nameof(Client.Id), nameof(Client.Name), ClientId);

            TypeOptions = new SelectList(AllowedTypes, Type);

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!string.IsNullOrWhiteSpace(Type) && !AllowedTypes.Contains(Type))
                ModelState.AddModelError(nameof(Type), "Please select a valid interaction type.");

            var effectiveUserId = _ctx.UserId!;

            if (!ModelState.IsValid)
            {
                var clientsQ = _context.Clients
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

                ClientsList = new SelectList(await clientsQ.OrderBy(c => c.Name).ToListAsync(),
                                             nameof(Client.Id), nameof(Client.Name), ClientId);
                TypeOptions = new SelectList(AllowedTypes, Type);
                return Page();
            }

            var entity = await _context.Interactions
                .FirstOrDefaultAsync(i => i.Id == Id && !i.IsDeleted);
            if (entity == null) return NotFound();

            var canEdit = await _authorization.AuthorizeAsync(User, entity, Operations.Edit);
            if (!canEdit.Succeeded) return Forbid();

            var clientExists = await _context.Clients
    .AnyAsync(c => c.Id == ClientId && !c.IsDeleted && c.UserId == effectiveUserId);

            if (!clientExists)
            {
                ModelState.AddModelError(nameof(ClientId), "Please select a valid client.");
                await RefillDropdownsAsync(effectiveUserId);
                return Page();
            }

            if (DealId.HasValue)
            {
                var dealExists = await _context.Deals
                    .AnyAsync(d => d.Id == DealId.Value
                                   && d.ClientId == ClientId
                                   && !d.IsDeleted
                                   && d.UserId == effectiveUserId);

                if (!dealExists)
                {
                    ModelState.AddModelError(nameof(DealId), "Please select a valid deal for this client.");
                    await RefillDropdownsAsync(effectiveUserId);
                    return Page();
                }
            }

            entity.ClientId = ClientId;
            entity.DealId = DealId;
            entity.Date = Date == default ? DateTime.Now : Date;
            entity.Type = Type?.Trim() ?? string.Empty;
            entity.Comment = Comment?.Trim();

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Update",
                InteractionId = entity.Id,
                UserId = _userManager.GetUserId(User),
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Interaction updated successfully!";
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnGetDealsAsync(int clientId)
        {
            var effectiveUserId = _ctx.UserId!;

            var clientOk = await _context.Clients
    .AnyAsync(c => c.Id == clientId && !c.IsDeleted && c.UserId == effectiveUserId);
            if (!clientOk) return new JsonResult(Array.Empty<object>());

            var deals = await _context.Deals
                .AsNoTracking()
                .Where(d => !d.IsDeleted
                            && d.ClientId == clientId
                            && d.UserId == effectiveUserId)
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .ThenBy(d => d.Title)
                .Select(d => new { id = d.Id, title = d.Title })
                .ToListAsync();

            return new JsonResult(deals);
        }

        private async Task RefillDropdownsAsync(string effectiveUserId)
        {
            var clientsQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

            ClientsList = new SelectList(await clientsQ.OrderBy(c => c.Name).ToListAsync(),
                                         nameof(Client.Id), nameof(Client.Name), ClientId);
            TypeOptions = new SelectList(AllowedTypes, Type);
        }
    }
}