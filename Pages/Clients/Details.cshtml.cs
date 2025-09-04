using System.Collections.Generic;
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
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Client Client { get; private set; } = null!;
        public IList<Interaction> Interactions { get; private set; } = new List<Interaction>();
        public IList<Deal> Deals { get; private set; } = new List<Deal>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var isAdmin = User.IsInRole("Admin");
            var currentUserId = _userManager.GetUserId(User);

            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.CreatedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client is null) return NotFound();
            Client = client;

            Interactions = await _context.Interactions
                .AsNoTracking()
                .Include(i => i.Deal)
                .Include(i => i.CreatedBy)
                .Where(i => i.ClientId == id
                            && !i.IsDeleted
                            && (isAdmin || i.UserId == currentUserId))
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            Deals = await _context.Deals
                .AsNoTracking()
                .Include(d => d.CreatedBy)
                .Where(d => d.ClientId == id
                            && !d.IsDeleted
                            && (isAdmin || d.UserId == currentUserId))
                .OrderByDescending(d => d.CloseDate)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            return Page();
        }
    }
}
