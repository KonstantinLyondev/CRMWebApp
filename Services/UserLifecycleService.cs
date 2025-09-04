using System;
using System.Linq;
using System.Threading.Tasks;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Services
{
    public class UserLifecycleService : IUserLifecycleService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        private const string AdminRole = "Admin";
        private const string SystemUserEmail = "system@crm.local";

        public UserLifecycleService(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<ApplicationUser> GetSystemUserAsync()
        {
            var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == SystemUserEmail);
            if (u == null) throw new Exception("System user not found.");
            return u;
        }

        public async Task<(bool ok, string? error)> DeactivateAsync(string userId, string? reason, string performedByAdminId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");
            if (user.Id == performedByAdminId) return (false, "You cannot deactivate your own account.");

            user.IsDeactivated = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateSecurityStampAsync(user);

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) return (false, string.Join("; ", res.Errors.Select(e => e.Description)));
            return (true, null);
        }

        public async Task<(bool ok, string? error)> ReactivateAsync(string userId, string performedByAdminId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            user.IsDeactivated = false;
            user.LockoutEnd = null;

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded) return (false, string.Join("; ", res.Errors.Select(e => e.Description)));
            return (true, null);
        }

        public async Task<(bool ok, string? error)> HardDeleteAsync(string userId, string performedByAdminId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "User not found.");
            if (user.Id == performedByAdminId) return (false, "You cannot delete your own account.");
            if (!user.IsDeactivated) return (false, "User must be deactivated before hard delete.");

            if (await _userManager.IsInRoleAsync(user, AdminRole))
            {
                var adminsCount = (await _userManager.GetUsersInRoleAsync(AdminRole))
                                  .Count(u => !u.IsDeactivated && u.Id != user.Id);
                if (adminsCount <= 0) return (false, "You cannot delete the last remaining Admin.");
            }

            var systemUser = await GetSystemUserAsync();
            var sysId = systemUser.Id;

                        await _db.Clients.IgnoreQueryFilters()
                .Where(c => c.CreatedById == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.CreatedById, sysId));
            await _db.Clients.IgnoreQueryFilters()
                .Where(c => c.UserId == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UserId, sysId));

            await _db.Deals.IgnoreQueryFilters()
                .Where(d => d.CreatedById == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.CreatedById, sysId));
            await _db.Deals.IgnoreQueryFilters()
                .Where(d => d.UserId == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.UserId, sysId));

            await _db.Interactions.IgnoreQueryFilters()
                .Where(i => i.CreatedById == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.CreatedById, sysId));
            await _db.Interactions.IgnoreQueryFilters()
                .Where(i => i.UserId == user.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.UserId, sysId));
            await _db.AuditLogs.IgnoreQueryFilters()
                            .Where(a => a.UserId == user.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(a => a.UserId, sysId));

            await _db.SaveChangesAsync(); 
                        var del = await _userManager.DeleteAsync(user);
            if (!del.Succeeded) return (false, string.Join("; ", del.Errors.Select(e => e.Description)));

            return (true, null);
        }
    }
}