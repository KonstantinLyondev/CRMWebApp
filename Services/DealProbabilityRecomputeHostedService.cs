using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRMWebApp.Data;
using CRMWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;  
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRMWebApp.Services
{
    public class DealProbabilityRecomputeOptions
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);
        public bool RunOnStartup { get; set; } = true;
    }
    public class DealProbabilityRecomputeHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<DealProbabilityRecomputeHostedService> _logger;
        private readonly DealProbabilityRecomputeOptions _opts;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public DealProbabilityRecomputeHostedService(
            IServiceProvider sp,
            ILogger<DealProbabilityRecomputeHostedService> logger,
            IOptions<DealProbabilityRecomputeOptions> opts)
        {
            _sp = sp;
            _logger = logger;
            _opts = opts.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_opts.RunOnStartup)
                await SafeRunOnceAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(_opts.Interval, stoppingToken); }
                catch (TaskCanceledException) { break; }
                await SafeRunOnceAsync(stoppingToken);
            }
        }

        private async Task SafeRunOnceAsync(CancellationToken ct)
        {
            if (!await _gate.WaitAsync(0, ct)) return;

            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scoring = scope.ServiceProvider.GetRequiredService<IDealScoringService>();

                var deals = await db.Deals
                    .Where(d => d.Status == DealStatus.InProgress && !d.IsDeleted)
                    .ToListAsync(ct);

                int updated = 0;
                foreach (var d in deals)
                {
                    var newProb = await scoring.ComputeProbabilityAsync(d);
                    if (d.Probability != newProb) { d.Probability = newProb; updated++; }
                }

                if (updated > 0) await db.SaveChangesAsync(ct);
                _logger.LogInformation("Recompute done: considered {all}, updated {upd}", deals.Count, updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recompute failed");
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}