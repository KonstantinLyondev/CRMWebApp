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

namespace CRMWebApp.Pages.Interactions
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IUserContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(AppDbContext context, IUserContext ctx, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _ctx = ctx;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)] public int ClientId { get; set; }
        [BindProperty(SupportsGet = true)] public int? DealId { get; set; }

        [BindProperty] public DateTime Date { get; set; } = DateTime.Now;
        [BindProperty] public string Type { get; set; } = string.Empty;
        [BindProperty] public string? Comment { get; set; } = string.Empty;

        public SelectList ClientsList { get; set; } = default!;
        public SelectList TypeOptions { get; set; } = default!;

        private static readonly string[] AllowedTypes = new[]
        {
            "Phone Call", "Email", "Meeting", "Message", "Demo", "Follow-up", "Other"
        };

        public void OnGet()
        {
            var effectiveUserId = _ctx.UserId!;

            var clientsQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

            ClientsList = new SelectList(
                clientsQ.OrderBy(c => c.Name),
                "Id", "Name", ClientId == 0 ? null : ClientId);

            TypeOptions = new SelectList(AllowedTypes);
            Date = DateTime.Now;
        }

        public async Task<JsonResult> OnGetDealsAsync(int clientId)
        {
            var effectiveUserId = _ctx.UserId!;

            var ownsClient = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c => c.Id == clientId && c.UserId == effectiveUserId && !c.IsDeleted);

            if (!ownsClient)
                return new JsonResult(Array.Empty<object>());

            var deals = await _context.Deals
                .AsNoTracking()
                .Where(d => d.ClientId == clientId && d.UserId == effectiveUserId && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new { id = d.Id, title = d.Title })
                .ToListAsync();

            return new JsonResult(deals);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var effectiveUserId = _ctx.UserId!;

            var clientsReloadQ = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.UserId == effectiveUserId);

            ClientsList = new SelectList(
                clientsReloadQ.OrderBy(c => c.Name),
                "Id", "Name", ClientId == 0 ? null : ClientId);

            TypeOptions = new SelectList(AllowedTypes);

            if (!string.IsNullOrWhiteSpace(Type) && !AllowedTypes.Contains(Type))
                ModelState.AddModelError(nameof(Type), "Please select a valid interaction type.");

            if (!ModelState.IsValid)
                return Page();

            var clientOk = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c => c.Id == ClientId && c.UserId == effectiveUserId && !c.IsDeleted);

            if (!clientOk)
            {
                ModelState.AddModelError(nameof(ClientId), "Please select a valid client.");
                return Page();
            }

            if (DealId.HasValue)
            {
                var dealOk = await _context.Deals
                    .AsNoTracking()
                    .AnyAsync(d => d.Id == DealId.Value
                                   && d.ClientId == ClientId
                                   && d.UserId == effectiveUserId
                                   && !d.IsDeleted);

                if (!dealOk)
                {
                    ModelState.AddModelError(nameof(DealId), "Please select a valid deal for this client.");
                    return Page();
                }
            }

            var interaction = new Interaction
            {
                ClientId = ClientId,
                DealId = DealId,
                Date = Date == default ? DateTime.Now : Date,
                Type = Type,
                Comment = Comment,
                UserId = effectiveUserId,    
                CreatedById = effectiveUserId, 
                IsDeleted = false
            };

            _context.Interactions.Add(interaction);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Create",
                InteractionId = interaction.Id,              
                UserId = _userManager.GetUserId(User),       
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Interaction created successfully!";
            return RedirectToPage("./Index");
        }
    }
}