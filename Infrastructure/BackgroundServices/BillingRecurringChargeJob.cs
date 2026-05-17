using Application.Contractors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

/// <summary>
/// Runs once per day at 01:00 UTC and advances <c>BookingDetail.LastRecurringChargeDate</c>
/// for every active booking whose next billing-cycle boundary has passed.
///
/// Due amounts are always computed dynamically via <see cref="Domain.RecurringChargeCalculator"/>
/// on every due-report request; this job merely tracks which cycles have been
/// formally "acknowledged" and writes to the audit field.
/// </summary>
public sealed class BillingRecurringChargeJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillingRecurringChargeJob> _logger;

    // Run at 01:00 UTC every day
    private static readonly TimeOnly TargetTime = new(1, 0, 0);

    public BillingRecurringChargeJob(IServiceScopeFactory scopeFactory, ILogger<BillingRecurringChargeJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BillingRecurringChargeJob started.");

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
        _logger.LogInformation("BillingRecurringChargeJob: processing recurring charges as of {AsOf:O}", asOf);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IRecurringChargeService>();
            var count = await svc.ProcessRecurringChargesAsync(asOf, stoppingToken);
            _logger.LogInformation("BillingRecurringChargeJob: advanced LastRecurringChargeDate for {Count} booking detail(s).", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "BillingRecurringChargeJob: unhandled error during recurring-charge cycle.");
        }
    }

    /// <summary>Returns the delay from now until the next <see cref="TargetTime"/> (UTC).</summary>
    private static TimeSpan ComputeDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(TargetTime.ToTimeSpan());
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }
}
