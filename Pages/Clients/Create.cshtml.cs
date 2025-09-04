using System;
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
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Client Client { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Client.UserId");

            if (!ModelState.IsValid)
                return Page();

            Client.Name = Client.Name?.Trim() ?? string.Empty;
            Client.Email = Client.Email?.Trim() ?? string.Empty;
            Client.Phone = Client.Phone?.Trim() ?? string.Empty;
            Client.City = Client.City?.Trim() ?? string.Empty;
            Client.Company = Client.Company?.Trim() ?? string.Empty;
            Client.Address = Client.Address?.Trim();

            var currentUserId = _userManager.GetUserId(User)!;
            Client.UserId = currentUserId;
            Client.CreatedById = currentUserId;

            if (!User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(Client.Email))
            {
                var exists = await _context.Clients
                    .AsNoTracking()
                    .AnyAsync(c => !c.IsDeleted &&
                                   c.UserId == Client.UserId &&
                                   c.Email == Client.Email);

                if (exists)
                {
                    ModelState.AddModelError("Client.Email", "A client with this email already exists.");
                    return Page();
                }
            }

            _context.Clients.Add(Client);
            await _context.SaveChangesAsync(); 

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Create",
                ClientId = Client.Id,       
                UserId = currentUserId,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Client created successfully!";
            return RedirectToPage("./Index");
        }
    }
}
