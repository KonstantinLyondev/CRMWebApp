using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMWebApp.Authorization;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Deals
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthorizationService _authorization;

        public IndexModel(AppDbContext context,
                          UserManager<ApplicationUser> userManager,
                          IAuthorizationService authorization)
        {
            _context = context;
            _userManager = userManager;
            _authorization = authorization;
        }

        [BindProperty(SupportsGet = true)]
        public DealStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CurrentSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Scope { get; set; } = "all"; 

        public IList<Row> Rows { get; private set; } = new List<Row>();

        public class Row
        {
            public Deal Item { get; set; } = null!;
            public bool CanEdit { get; set; }
            public bool CanDelete { get; set; }
        }

        public async Task OnGetAsync(string? scope = "all")
        {
            var query = BaseQueryFor(scope);
            query = ApplyFilterAndSort(query, Q, StatusFilter, CurrentSort);

            var items = await query.ToListAsync();
            Rows = await BuildRowsAsync(items);
        }

        public async Task<PartialViewResult> OnGetFilterAsync(
            string query, string? sortOrder, DealStatus? status, string? scope = "all")
        {
            var baseQ = BaseQueryFor(scope);
            baseQ = ApplyFilterAndSort(baseQ, query, status, sortOrder);

            var items = await baseQ.ToListAsync();
            var rows = await BuildRowsAsync(items);

            return Partial("_DealRows", rows);
        }

        private IQueryable<Deal> BaseQueryFor(string? scope)
        {
            var isAdmin = User.IsInRole("Admin");
            Scope = isAdmin && scope == "created" ? "created" : "all";

            var q = _context.Deals
                .AsNoTracking()
                .Include(d => d.Client)
                .Where(d => !d.IsDeleted);

            if (!isAdmin)
            {
                var uid = _userManager.GetUserId(User);
                q = q.Where(d => d.UserId == uid);
            }
            else if (Scope == "created")
            {
                var uid = _userManager.GetUserId(User);
                q = q.Where(d => d.CreatedById == uid);
            }

            return q;
        }

        private static IQueryable<Deal> ApplyFilterAndSort(
            IQueryable<Deal> q, string? query, DealStatus? status, string? sort)
        {
            if (status.HasValue)
            {
                var st = status.Value;
                q = q.Where(d => d.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                q = q.Where(d =>
                    EF.Functions.Like(d.Title, term + "%") ||
                    (d.Notes != null && EF.Functions.Like(d.Notes, term + "%")) ||
                    (d.Client != null && EF.Functions.Like(d.Client.Name, term + "%"))
                );
            }

            q = sort switch
            {
                "Title" => q.OrderBy(d => d.Title),
                "Title_desc" => q.OrderByDescending(d => d.Title),
                "Amount" => q.OrderBy(d => d.Amount),
                "Amount_desc" => q.OrderByDescending(d => d.Amount),
                _ => q.OrderByDescending(d => d.CreatedAt)
            };

            return q;
        }

        private async Task<IList<Row>> BuildRowsAsync(IEnumerable<Deal> items)
        {
            var list = new List<Row>();
            foreach (var it in items)
            {
                var canEdit = await _authorization.AuthorizeAsync(User, it, Operations.Edit);
                var canDelete = await _authorization.AuthorizeAsync(User, it, Operations.Delete);

                list.Add(new Row
                {
                    Item = it,
                    CanEdit = canEdit.Succeeded,
                    CanDelete = canDelete.Succeeded
                });
            }
            return list;
        }
    }
}
