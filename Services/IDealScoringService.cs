using System.Threading.Tasks;
using CRMWebApp.Models;

namespace CRMWebApp.Services
{
    public interface IDealScoringService
    {
        Task<byte> ComputeProbabilityAsync(Deal deal);
    }
}
