using Application.Contractors;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

/// <summary>
/// Keeps the charge ledger (RecurringChargeEntries) current: once shortly after start-up (catch-up)
/// and then every day just after local midnight, it posts every billing month that has started,
/// plus labour/adjustments/corrections, for all active bookings (see BookingLedgerService).
/// Dues are read from the ledger, so this runs regardless of the company's recurring-charge switch.
/// </summary>
public sealed class BillingRecurringChargeJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillingRecurringChargeJob> _logger;

    // Run at 00:05 server-local time every day (new billing months start at local midnight)
    private static readonly TimeOnly TargetTime = new(0, 5, 0);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    public BillingRecurringChargeJob(IServiceScopeFactory scopeFactory, ILogger<BillingRecurringChargeJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BillingRecurringChargeJob started.");

        // Catch up after a restart (missed nights are posted on their real dates)
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
            await RunRecurringChargeCycleAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelayUntilNextRun();
            _logger.LogInformation("BillingRecurringChargeJob: next run in {Delay:hh\\:mm\\:ss}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunRecurringChargeCycleAsync(stoppingToken);
        }

        _logger.LogInformation("BillingRecurringChargeJob stopped.");
    }

    private async Task RunRecurringChargeCycleAsync(CancellationToken stoppingToken)
    {
        var asOf = DateTime.UtcNow;
        _logger.LogInformation("BillingRecurringChargeJob: checking recurring charges configuration as of {AsOf:O}", asOf);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IRecurringChargeService>();
            var count = await svc.ProcessRecurringChargesAsync(asOf, stoppingToken);
            _logger.LogInformation("BillingRecurringChargeJob: charge ledger synced, {Count} entr(ies) posted.", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "BillingRecurringChargeJob: unhandled error during recurring-charge cycle.");
        }
    }

    /// <summary>Returns the delay from now until the next <see cref="TargetTime"/> (server-local time).</summary>
    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.Add(TargetTime.ToTimeSpan());
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }
}
