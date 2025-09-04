using System.Threading.Tasks;

namespace CRMWebApp.Services
{
    public interface IUserLifecycleService
    {
        Task<(bool ok, string? error)> DeactivateAsync(string userId, string? reason, string performedByAdminId);
        Task<(bool ok, string? error)> ReactivateAsync(string userId, string performedByAdminId);
        Task<(bool ok, string? error)> HardDeleteAsync(string userId, string performedByAdminId);
    }
}