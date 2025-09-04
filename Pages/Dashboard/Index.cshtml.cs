using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CRMWebApp.Data;
using CRMWebApp.ViewModels;
using CRMWebApp.Models; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Dashboard
{
    [Authorize] 
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string Scope { get; set; } = "all"; 

        public DashboardViewModel VM { get; set; } = new();

        private bool UseMineOnly(bool isAdmin) => !isAdmin || string.Equals(Scope, "mine", StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            var uid = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var mineOnly = UseMineOnly(isAdmin);

                        var clientsQ = _context.Clients.AsNoTracking().Where(c => !c.IsDeleted);
            if (mineOnly) clientsQ = clientsQ.Where(c => c.UserId == uid);
            VM.TotalClients = await clientsQ.CountAsync();

            var dealsQ = _context.Deals.AsNoTracking().Include(d => d.Client)
                .Where(d => !d.IsDeleted && !d.Client.IsDeleted);
            if (mineOnly) dealsQ = dealsQ.Where(d => d.UserId == uid);
            VM.TotalDeals = await dealsQ.CountAsync();

            var interQ = _context.Interactions.AsNoTracking().Where(i => !i.IsDeleted);
            if (mineOnly) interQ = interQ.Where(i => i.UserId == uid);
            VM.TotalInteractions = await interQ.CountAsync();

            VM.OpenDeals = await dealsQ.Where(d => d.Status == DealStatus.InProgress).CountAsync();

                        var recentQ = _context.Interactions.AsNoTracking().Where(i => !i.IsDeleted);
            if (mineOnly) recentQ = recentQ.Where(i => i.UserId == uid);

            VM.RecentInteractions = await recentQ
                .Include(i => i.Client)
                .Include(i => i.Deal)
                .OrderByDescending(i => i.Date)
                .Take(5)
                .Select(i => new RecentInteractionRow
                {
                    Id = i.Id,
                    Date = i.Date,
                    ClientName = i.Client != null ? i.Client.Name : $"Client #{i.ClientId}",
                    DealTitle = i.Deal != null
                        ? (i.Deal.Title ?? $"Deal #{i.DealId}")
                        : (i.DealId != null ? $"Deal #{i.DealId}" : "—"),
                    Type = i.Type ?? "—",
                    PerformedBy = "—"
                })
                .ToListAsync();
        }

        public class SalesSeries
        {
            public string Label { get; set; } = string.Empty;
            public decimal[] Data { get; set; } = Array.Empty<decimal>();
        }

        public class SalesChartPayload
        {
            public string[] Labels { get; set; } = Array.Empty<string>();
            public List<SalesSeries> Series { get; set; } = new();
        }

        public async Task<IActionResult> OnGetSalesDataAsync(string range = "12m", string? scope = null)
        {
            var uid = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var effectiveScope = string.IsNullOrWhiteSpace(scope) ? Scope : scope;
            var mineOnly = !isAdmin || string.Equals(effectiveScope, "mine", StringComparison.OrdinalIgnoreCase);

            var months = range == "3m" ? 3 : range == "6m" ? 6 : 12;
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(months - 1));
            var end = start.AddMonths(months);

            var baseDealsQ = _context.Deals.AsNoTracking().Include(d => d.Client)
                .Where(d => !d.IsDeleted && !d.Client.IsDeleted);
            if (mineOnly) baseDealsQ = baseDealsQ.Where(d => d.UserId == uid);

            var createdDates = await baseDealsQ
                .Where(d => d.CreatedAt >= start && d.CreatedAt < end)
                .Select(d => d.CreatedAt)
                .ToListAsync();

            var wonDates = await baseDealsQ
                .Where(d => d.Status == DealStatus.Successful &&
                            d.CloseDate.HasValue &&
                            d.CloseDate.Value >= start &&
                            d.CloseDate.Value < end)
                .Select(d => d.CloseDate!.Value)
                .ToListAsync();

            var lostDates = await baseDealsQ
                .Where(d => d.Status == DealStatus.Failed &&
                            d.CloseDate.HasValue &&
                            d.CloseDate.Value >= start &&
                            d.CloseDate.Value < end)
                .Select(d => d.CloseDate!.Value)
                .ToListAsync();

            var labels = new List<string>();
            var createdCounts = new List<decimal>();
            var wonCounts = new List<decimal>();
            var lostCounts = new List<decimal>();

            for (var dt = start; dt < end; dt = dt.AddMonths(1))
            {
                labels.Add(dt.ToString("yyyy-MM"));
                createdCounts.Add(createdDates.Count(x => x.Year == dt.Year && x.Month == dt.Month));
                wonCounts.Add(wonDates.Count(x => x.Year == dt.Year && x.Month == dt.Month));
                lostCounts.Add(lostDates.Count(x => x.Year == dt.Year && x.Month == dt.Month));
            }

            return new JsonResult(new SalesChartPayload
            {
                Labels = labels.ToArray(),
                Series = new List<SalesSeries>
                {
                    new SalesSeries { Label = "Deals Created", Data = createdCounts.ToArray() },
                    new SalesSeries { Label = "Deals Won",     Data = wonCounts.ToArray() },
                    new SalesSeries { Label = "Deals Lost",    Data = lostCounts.ToArray() }
                }
            });
        }
    }
}
