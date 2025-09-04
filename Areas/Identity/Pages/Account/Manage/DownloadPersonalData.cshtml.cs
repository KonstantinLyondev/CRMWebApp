using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using CRMWebApp.Data;
using CRMWebApp.Models;

namespace CRMWebApp.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;
        private readonly ILogger<DownloadPersonalDataModel> _logger;

        public DownloadPersonalDataModel(
            UserManager<ApplicationUser> userManager,
            AppDbContext db,
            ILogger<DownloadPersonalDataModel> logger)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            _logger.LogInformation("User with ID '{UserId}' requested their personal data.", user.Id);

            var userBlock = new
            {
                user.Id,
                UserName = await _userManager.GetUserNameAsync(user),
                Email = await _userManager.GetEmailAsync(user),
                EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user),
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
            };

            var userId = user.Id;
            var clients = await _db.Clients
                .AsNoTracking()
                .Where(c => c.UserId == userId) 
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    c.Phone,
                    c.City,
                    c.Company,
                    c.UserId,
                    c.IsDeleted,
                    c.Address,
                    c.IsVip
                })
                .ToListAsync();

            var clientIds = clients.Select(c => c.Id).ToList();
            var deals = await _db.Deals
                .AsNoTracking()
                .Where(d => d.UserId == userId && clientIds.Contains(d.ClientId))
                .OrderBy(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.ClientId,
                    d.Status,
                    d.Amount,
                    d.Currency,
                    d.Probability,
                    d.CloseDate,
                    d.Notes,
                    d.UserId,
                    d.IsDeleted,
                    d.CreatedAt,
                    d.UpdatedAt
                })
                .ToListAsync();

            var interactions = await _db.Interactions
                .AsNoTracking()
                .Where(i => i.UserId == userId && clientIds.Contains(i.ClientId))
                .OrderByDescending(i => i.Date)
                .Select(i => new
                {
                    i.Id,
                    i.ClientId,
                    i.Date,
                    i.Type,
                    i.Comment,
                    i.UserId,
                    i.IsDeleted,
                    i.DealId
                })
                .ToListAsync();

            var clientsWithChildren = clients
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    c.Phone,
                    c.City,
                    c.Company,
                    c.UserId,
                    c.IsDeleted,
                    c.Address,
                    c.IsVip,
                    Deals = deals.Where(d => d.ClientId == c.Id).ToList(),
                    Interactions = interactions.Where(i => i.ClientId == c.Id).ToList()
                })
                .ToList();

            var export = new
            {
                User = userBlock,
                Clients = clientsWithChildren
            };

            var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(Encoding.UTF8.GetBytes(json), "application/json");
        }
    }
}