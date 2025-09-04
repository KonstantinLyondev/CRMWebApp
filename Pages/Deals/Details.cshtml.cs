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

namespace CRMWebApp.Pages.Deals
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

        public Deal Deal { get; private set; } = null!;
        public IList<Interaction> Interactions { get; private set; } = new List<Interaction>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var isAdmin = User.IsInRole("Admin");
            var currentUserId = _userManager.GetUserId(User);

            Deal = await _context.Deals
                .AsNoTracking()
                .Include(d => d.Client)
                .Include(d => d.CreatedBy) 
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    !d.IsDeleted &&
                    (isAdmin || d.UserId == currentUserId));

            if (Deal is null) return NotFound();

            Interactions = await _context.Interactions
                .AsNoTracking()
                .Include(i => i.CreatedBy)
                .Where(i =>
                    !i.IsDeleted &&
                    i.DealId == id &&
                    (isAdmin || i.UserId == currentUserId))
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            return Page();
        }
    }
}