using Application.Contractors;
using Application.ReponseDTO;
using Application.Repositories;
using Application.Services.Common;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;

    public DashboardService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<Transaction, Guid> transactionRepository,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _transactionRepository = transactionRepository;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
    }

    public async Task<DashboardStatsResponse> GetDashboardStatsAsync(
        DateTime startDate,
        DateTime endDate,
        int? branchId = null,
        CancellationToken cancellationToken = default)
    {
        // Proper UTC time handling
        var fromLocal = startDate.Date;
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
            .ToUniversalTime();

        var toLocal = endDate.Date.AddDays(1);
        var toUtc = DateTime.SpecifyKind(toLocal, DateTimeKind.Local)
            .ToUniversalTime();

        // Build base queries filtered by tenant and date range
        var bookingsQuery = _bookingRepository.Query()
            .Where(x => x.TenantId == _tenantId
                && x.BookingDate >= fromUtc
                && x.BookingDate < toUtc);

        var deliveriesQuery = _deliveryRepository.Query()
            .Where(x => x.TenantId == _tenantId
                && x.DeliveryDate >= fromUtc
                && x.DeliveryDate < toUtc);

        var transactionsQuery = _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(x => x.TenantId == _tenantId
                && x.TransactionDate >= fromUtc
                && x.TransactionDate < toUtc
                && !x.IsDeleted
                && !x.IsArchived
                && x.PaymentMethod != PaymentMethods.CREDIT);

        // Apply branch filter if provided
        if (branchId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(x => x.BranchId == branchId.Value);
            deliveriesQuery = deliveriesQuery.Where(x => x.BranchId == branchId.Value);
            transactionsQuery = transactionsQuery.Where(x => x.BranchId == branchId.Value);
        }

        // Get booking statistics
        var totalBookings = await bookingsQuery.CountAsync(cancellationToken);
        var totalBookingAmount = await bookingsQuery
            .SelectMany(b => b.BookingDetails)
            .SumAsync(d => (decimal)d.BookingQuantity * d.BookingRate, cancellationToken);

        // Get delivery statistics
        var totalDeliveries = await deliveriesQuery.CountAsync(cancellationToken);
        var totalDeliveryAmount = await deliveriesQuery
            .SelectMany(d => d.DeliveryDetails)
            .SumAsync(d => (decimal)d.DeliveryQuantity * d.BookingDetail!.BookingRate, cancellationToken);

        // Get bill collection statistics (from transactions with BILL_COLLECTION type)
        var billCollectionQuery = transactionsQuery.Where(x => x.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION);
        var totalBillCollections = await billCollectionQuery.CountAsync(cancellationToken);
        var totalBillCollectionAmount = await billCollectionQuery
            .SumAsync(bc => bc.NetAmount, cancellationToken);

        // Get revenue from transactions (IN flow = revenue, OUT flow = expense)
        var revenueTransactions = await transactionsQuery
            .Where(x => x.TransactionHead!.Type == TransactionHeadTypes.DEBIT)
            .SumAsync(x => x.NetAmount, cancellationToken);

        var expenseTransactions = await transactionsQuery
            .Where(x => x.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
            .SumAsync(x => Math.Abs(x.NetAmount), cancellationToken);

        var netRevenue = revenueTransactions - expenseTransactions;
        var periodDays = (endDate.Date - startDate.Date).Days + 1;

        return new DashboardStatsResponse(
            TotalBookings: totalBookings,
            TotalBookingAmount: totalBookingAmount,
            TotalDeliveries: totalDeliveries,
            TotalDeliveryAmount: totalDeliveryAmount,
            TotalBillCollections: totalBillCollections,
            TotalBillCollectionAmount: totalBillCollectionAmount,
            TotalRevenue: revenueTransactions,
            TotalExpense: expenseTransactions,
            NetRevenue: netRevenue,
            StartDate: startDate,
            EndDate: endDate,
            PeriodDays: periodDays
        );
    }

    public async Task<DashboardTrendsResponse> GetDashboardTrendsAsync(
        int periodDays,
        int? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var endLocal = DateTime.Now.Date;
        var startLocal = endLocal.AddDays(-periodDays + 1);

        // Proper UTC time handling
        var fromUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local)
            .ToUniversalTime();
        var toUtc = DateTime.SpecifyKind(endLocal.AddDays(1), DateTimeKind.Local)
            .ToUniversalTime();

        // Base queries
        var bookingsQuery = _bookingRepository.Query()
            .Where(x => x.TenantId == _tenantId
                && x.BookingDate >= fromUtc
                && x.BookingDate < toUtc);

        var deliveriesQuery = _deliveryRepository.Query()
            .Where(x => x.TenantId == _tenantId
                && x.DeliveryDate >= fromUtc
                && x.DeliveryDate < toUtc);

        var transactionsQuery = _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(x => x.TenantId == _tenantId && !x.IsDeleted && !x.IsArchived && x.PaymentMethod != PaymentMethods.CREDIT &&
                        x.TransactionDate >= fromUtc && x.TransactionDate < toUtc &&
                        x.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                        x.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE);

        // Apply branch filter
        if (branchId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(x => x.BranchId == branchId.Value);
            deliveriesQuery = deliveriesQuery.Where(x => x.BranchId == branchId.Value);
            transactionsQuery = transactionsQuery.Where(x => x.BranchId == branchId.Value);
        }

        // Get all data
        var bookings = await bookingsQuery.ToListAsync(cancellationToken);
        var deliveries = await deliveriesQuery.ToListAsync(cancellationToken);
        var transactions = await transactionsQuery.ToListAsync(cancellationToken);

        // Generate date labels
        var dateLabels = new List<string>();
        var groupingDays = periodDays <= 15 ? 1 : periodDays <= 30 ? 2 : periodDays <= 90 ? 7 : 30;

        // Revenue trend (IN transactions) - convert to local time for grouping
        var revenueTrend = transactions
            .Where(t => t.TransactionHead!.Type == TransactionHeadTypes.DEBIT)
            .GroupBy(t => t.TransactionDate.ToLocalTime().Date)
            .Select(g => new DailyTrendData(g.Key, g.Sum(x => x.NetAmount), g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        // Expense trend (OUT transactions) - convert to local time for grouping
        var expenseTrend = transactions
            .Where(t => t.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
            .GroupBy(t => t.TransactionDate.ToLocalTime().Date)
            .Select(g => new DailyTrendData(g.Key, Math.Abs(g.Sum(x => x.NetAmount)), g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        // Net profit trend
        var netProfitTrend = new List<DailyTrendData>();
        var allDates = revenueTrend.Select(r => r.Date)
            .Union(expenseTrend.Select(e => e.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        foreach (var date in allDates)
        {
            var revenue = revenueTrend.FirstOrDefault(r => r.Date == date)?.Amount ?? 0;
            var expense = expenseTrend.FirstOrDefault(e => e.Date == date)?.Amount ?? 0;
            netProfitTrend.Add(new DailyTrendData(date, revenue - expense, 0));
        }

        // Booking trend - convert to local time for grouping
        var bookingTrend = bookings
            .GroupBy(b => b.BookingDate.ToLocalTime().Date)
            .Select(g => new DailyTrendData(
                g.Key,
                g.SelectMany(b => b.BookingDetails).Sum(d => (decimal)d.BookingQuantity * d.BookingRate),
                g.Count()
            ))
            .OrderBy(d => d.Date)
            .ToList();

        // Delivery trend - convert to local time for grouping
        var deliveryTrend = deliveries
            .GroupBy(d => d.DeliveryDate.ToLocalTime().Date)
            .Select(g => new DailyTrendData(
                g.Key,
                g.SelectMany(d => d.DeliveryDetails).Sum(dd => (decimal)dd.DeliveryQuantity * dd.BookingDetail!.BookingRate),
                g.Count()
            ))
            .OrderBy(d => d.Date)
            .ToList();

        // Transaction category trends (for stacked bar chart)
        var categoryTrends = new Dictionary<string, List<decimal>>();
        var transactionTypes = new[] {
            UsageFor.BILL_COLLECTION,
            UsageFor.SALARY,
            UsageFor.TRANSACTION
        };

        // Group data by date intervals for better visualization
        var groupedDates = GenerateDateGroups(startLocal, endLocal, groupingDays);

        foreach (var type in transactionTypes)
        {
            var typeData = new List<decimal>();
            foreach (var dateGroup in groupedDates)
            {
                var amount = transactions
                    .Where(t => t.TransactionHead!.UsageFor == type)
                    .Where(t =>
                    {
                        var localDate = t.TransactionDate.ToLocalTime().Date;
                        return localDate >= dateGroup.Start && localDate < dateGroup.End;
                    })
                    .Sum(t => Math.Abs(t.NetAmount));
                typeData.Add(amount);
            }
            categoryTrends[type] = typeData;
        }

        // Generate labels based on grouping
        dateLabels = groupedDates.Select(g =>
            groupingDays == 1 ? g.Start.ToString("MMM dd") :
            groupingDays <= 7 ? $"{g.Start:MMM dd}" :
            $"{g.Start:MMM dd}-{g.End.AddDays(-1):dd}"
        ).ToList();

        return new DashboardTrendsResponse(
            RevenueTrend: revenueTrend,
            ExpenseTrend: expenseTrend,
            NetProfitTrend: netProfitTrend,
            BookingTrend: bookingTrend,
            DeliveryTrend: deliveryTrend,
            TransactionCategoryTrends: categoryTrends,
            DateLabels: dateLabels
        );
    }

    private List<(DateTime Start, DateTime End)> GenerateDateGroups(DateTime startDate, DateTime endDate, int groupingDays)
    {
        var groups = new List<(DateTime Start, DateTime End)>();
        var current = startDate;

        while (current < endDate)
        {
            var groupEnd = current.AddDays(groupingDays);
            if (groupEnd > endDate) groupEnd = endDate;
            groups.Add((current, groupEnd));
            current = groupEnd;
        }

        return groups;
    }
}
