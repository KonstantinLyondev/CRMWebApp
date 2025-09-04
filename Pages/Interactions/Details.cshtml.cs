using System.Threading.Tasks;
using System.Linq;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Pages.Interactions
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

        public Interaction Interaction { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0) return NotFound();

            var isAdmin = User.IsInRole("Admin");
            var query = _context.Interactions
                .AsNoTracking()
                .Include(i => i.Client)
                .Include(i => i.Deal)
                .Include(i => i.CreatedBy) 
                .Where(i => i.Id == id && !i.IsDeleted);

            if (!isAdmin)
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(i => i.UserId == currentUserId);
            }

            Interaction = await query.FirstOrDefaultAsync();
            if (Interaction == null) return NotFound();

            return Page();
        }
    }
}
