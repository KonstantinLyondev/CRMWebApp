using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Admin.AuditLogs
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public IList<Row> Logs { get; set; } = new List<Row>();

        public class Row
        {
            public DateTime Time { get; set; }
            public string UserEmail { get; set; } = "";
            public string Action { get; set; } = "";
            public string EntityType { get; set; } = "";
            public string Record { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
                        var raw = await _context.AuditLogs
                .AsNoTracking()
                .Include(l => l.User)
                .Include(l => l.Client)
                .Include(l => l.Deal)
                .Include(l => l.Interaction)
                .OrderByDescending(l => l.Timestamp)
                .Take(500)
                .ToListAsync();

            Logs = raw.Select(l =>
            {
                var entityType = l.ClientId.HasValue ? "Client"
                              : l.DealId.HasValue ? "Deal"
                              : l.InteractionId.HasValue ? "Interaction" : "?";

                var record = entityType switch
                {
                    "Client" => l.Client?.Name ?? $"Client #{l.ClientId}",
                    "Deal" => l.Deal?.Title ?? $"Deal #{l.DealId}",
                    "Interaction" => l.Interaction != null
                        ? $"{(l.Interaction.Type ?? "Interaction")} ({l.Interaction.Date:yyyy-MM-dd})"
                        : $"Interaction #{l.InteractionId}",
                    _ => "#?"
                };

                return new Row
                {
                    Time = l.Timestamp,
                    UserEmail = l.User?.Email ?? l.UserId ?? "",
                    Action = l.Action,
                    EntityType = entityType,
                    Record = record
                };
            }).ToList();
        }
    }
}
