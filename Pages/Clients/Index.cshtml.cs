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

namespace CRMWebApp.Pages.Clients
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
        public string? CurrentSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Scope { get; set; } = "all"; 

        public IList<Row> Rows { get; private set; } = new List<Row>();

        public class Row
        {
            public Client Item { get; set; } = null!;
            public bool CanEdit { get; set; }
            public bool CanDelete { get; set; }
        }

        public async Task OnGetAsync(string? scope = "all")
        {
            var q = BaseQueryFor(scope);
            q = ApplySortAndSearch(q, null, CurrentSort);

            var items = await q.ToListAsync();
            Rows = await BuildRowsAsync(items);
        }

        public async Task<PartialViewResult> OnGetFilterAsync(string query, string? sortOrder, string? scope = "all")
        {
            var q = BaseQueryFor(scope);
            q = ApplySortAndSearch(q, query, sortOrder);

            var items = await q.ToListAsync();
            var rows = await BuildRowsAsync(items);

            return Partial("_ClientRows", rows);
        }
        private IQueryable<Client> BaseQueryFor(string? scope)
        {
            var isAdmin = User.IsInRole("Admin");
            Scope = isAdmin && scope == "created" ? "created" : "all";

            var clients = _context.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            if (!isAdmin)
            {
                var uid = _userManager.GetUserId(User);
                clients = clients.Where(c => c.UserId == uid);
            }
            else if (Scope == "created")
            {
                var uid = _userManager.GetUserId(User);
                clients = clients.Where(c => c.CreatedById == uid);
            }

            return clients;
        }

        private static IQueryable<Client> ApplySortAndSearch(IQueryable<Client> q, string? search, string? sort)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(c => c.Name.StartsWith(term));
            }

            q = sort switch
            {
                "Name" => q.OrderBy(c => c.Name),
                "Name_desc" => q.OrderByDescending(c => c.Name),
                "VipOnly" => q.Where(c => c.IsVip).OrderBy(c => c.Name),
                _ => q.OrderByDescending(c => c.Id)
            };

            return q;
        }

        private async Task<IList<Row>> BuildRowsAsync(IEnumerable<Client> items)
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
