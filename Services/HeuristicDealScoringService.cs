using System;
using System.Linq;
using System.Threading.Tasks;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Services
{
    public class HeuristicDealScoringService : IDealScoringService
    {
        private readonly AppDbContext _db;
        public HeuristicDealScoringService(AppDbContext db) => _db = db;

        public async Task<byte> ComputeProbabilityAsync(Deal deal)
        {
            if (deal.Status == DealStatus.Successful) return 100;
            if (deal.Status == DealStatus.Failed) return 0;

            double p = 40; 

            if (deal.ClientId != 0)
            {
                var isVip = await _db.Clients
                    .Where(c => c.Id == deal.ClientId)
                    .Select(c => c.IsVip)
                    .FirstOrDefaultAsync();
                if (isVip) p += 10;
            }

            if (!string.IsNullOrEmpty(deal.UserId))
            {
                var oneYearAgo = DateTime.UtcNow.AddMonths(-12);

                var ownerWins = await _db.Deals.CountAsync(d =>
                    d.UserId == deal.UserId &&
                    d.CreatedAt >= oneYearAgo &&
                    d.Status == DealStatus.Successful);

                var ownerTotal = await _db.Deals.CountAsync(d =>
                    d.UserId == deal.UserId &&
                    d.CreatedAt >= oneYearAgo &&
                    d.Status != DealStatus.Failed);

                var ownerRate = ownerTotal > 0 ? (double)ownerWins / ownerTotal : 0.0;

                var allWins = await _db.Deals.CountAsync(d =>
                    d.CreatedAt >= oneYearAgo &&
                    d.Status == DealStatus.Successful);

                var allTotal = await _db.Deals.CountAsync(d =>
                    d.CreatedAt >= oneYearAgo &&
                    d.Status != DealStatus.Failed);

                var avgRate = allTotal > 0 ? (double)allWins / allTotal : 0.0;

                p += (ownerRate - avgRate) * 20.0; 
            }

            if (deal.CloseDate.HasValue)
            {
                var days = (deal.CloseDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
                if (days <= 7 && days >= 0) p += 5;
                if (days < 0) p -= 10; 
            }

            if (deal.Amount >= 10000) p += 5;
            else if (deal.Amount <= 200) p -= 5;

            p = Math.Max(0, Math.Min(100, p));
            return (byte)Math.Round(p);
        }
    }
}