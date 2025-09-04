using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMWebApp.Authorization;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Interactions
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
        public string? SortBy { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Scope { get; set; } = "all"; 
        public IList<Row> Rows { get; private set; } = new List<Row>();

        public class Row
        {
            public Interaction Item { get; set; } = null!;
            public bool CanEdit { get; set; }
            public bool CanDelete { get; set; }
        }

        public async Task OnGetAsync(string? scope = "all")
        {
            var baseQuery = BaseQueryFor(scope);
            baseQuery = ApplySort(baseQuery, SortBy);

            var items = await baseQuery.ToListAsync();
            Rows = await BuildRowsAsync(items);
        }

        public async Task<PartialViewResult> OnGetFilterAsync(string query, string? sortOrder, string? scope = "all")
        {
            var baseQuery = BaseQueryFor(scope);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var pattern = query + "%";
                baseQuery = baseQuery.Where(i =>
                    EF.Functions.Like(i.Type!, pattern) ||
                    EF.Functions.Like(i.Client!.Name!, pattern));
            }

            baseQuery = ApplySort(baseQuery, sortOrder);

            var items = await baseQuery.ToListAsync();
            var rows = await BuildRowsAsync(items);

                        return Partial("_InteractionRows", rows);
        }

                private IQueryable<Interaction> BaseQueryFor(string? scope)
        {
            var isAdmin = User.IsInRole("Admin");
            Scope = isAdmin && scope == "created" ? "created" : "all";

            var query = _context.Interactions
                .AsNoTracking()
                .Include(i => i.Client)
                .Include(i => i.Deal)
                .Where(i => !i.IsDeleted);

            if (!isAdmin)
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(i => i.UserId == currentUserId);
            }
            else if (Scope == "created")
            {
                var uid = _userManager.GetUserId(User);
                query = query.Where(i => i.CreatedById == uid);
            }

            return query;
        }

        private static IQueryable<Interaction> ApplySort(IQueryable<Interaction> q, string? sort)
        {
            return sort switch
            {
                "date_asc" => q.OrderBy(i => i.Date),
                "date_desc" => q.OrderByDescending(i => i.Date),
                "type_asc" => q.OrderBy(i => i.Type),
                "type_desc" => q.OrderByDescending(i => i.Type),
                "client_asc" => q.OrderBy(i => i.Client!.Name),
                "client_desc" => q.OrderByDescending(i => i.Client!.Name),
                _ => q.OrderByDescending(i => i.Date)
            };
        }

        private async Task<IList<Row>> BuildRowsAsync(IEnumerable<Interaction> items)
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
