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
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Client Client { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) return NotFound();

            Client = client;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Client.UserId");

            if (!ModelState.IsValid)
                return Page();

            var clientToUpdate = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == Client.Id);

            if (clientToUpdate == null)
                return NotFound();

            var normalizedName = Client.Name?.Trim() ?? string.Empty;
            var normalizedEmail = Client.Email?.Trim() ?? string.Empty;
            var normalizedPhone = Client.Phone?.Trim() ?? string.Empty;
            var normalizedCity = Client.City?.Trim() ?? string.Empty;
            var normalizedCompany = Client.Company?.Trim() ?? string.Empty;
            var normalizedAddress = Client.Address?.Trim();

            if (!User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var duplicate = await _context.Clients
                    .AsNoTracking()
                    .AnyAsync(c => !c.IsDeleted
                                   && c.UserId == clientToUpdate.UserId
                                   && c.Email == normalizedEmail
                                   && c.Id != clientToUpdate.Id);

                if (duplicate)
                {
                    ModelState.AddModelError("Client.Email", "A client with this email already exists.");
                    return Page();
                }
            }

            clientToUpdate.Name = normalizedName;
            clientToUpdate.Email = normalizedEmail;
            clientToUpdate.Phone = normalizedPhone;
            clientToUpdate.City = normalizedCity;
            clientToUpdate.Address = normalizedAddress;
            clientToUpdate.Company = normalizedCompany;
            clientToUpdate.IsVip = Client.IsVip;

            try
            {
                await _context.SaveChangesAsync();

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Update",
                    ClientId = clientToUpdate.Id,           
                    UserId = _userManager.GetUserId(User),  
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var stillExists = await _context.Clients.AnyAsync(c => c.Id == Client.Id);
                if (!stillExists) return NotFound();
                throw;
            }

            TempData["Success"] = "Client updated successfully!";
            return RedirectToPage("./Index");
        }
    }
}

