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
        // Ensure dates include full day ranges
        startDate = startDate.Date;
        endDate = endDate.Date.AddDays(1).AddSeconds(-1);

        // Build base queries filtered by tenant and date range
        var bookingsQuery = _bookingRepository.Query()
            .Where(x => x.TenantId == _tenantId 
                && x.BookingDate >= startDate 
                && x.BookingDate <= endDate);

        var deliveriesQuery = _deliveryRepository.Query()
            .Where(x => x.TenantId == _tenantId 
                && x.DeliveryDate >= startDate 
                && x.DeliveryDate <= endDate);

        var transactionsQuery = _transactionRepository.Query()
            .Where(x => x.TenantId == _tenantId 
                && x.TransactionDate >= startDate 
                && x.TransactionDate <= endDate);

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
        var billCollectionQuery = transactionsQuery.Where(x => x.TransactionType == TransactionTypes.BILL_COLLECTION);
        var totalBillCollections = await billCollectionQuery.CountAsync(cancellationToken);
        var totalBillCollectionAmount = await billCollectionQuery
            .SumAsync(bc => bc.NetAmount, cancellationToken);

        // Get revenue from transactions (IN flow = revenue, OUT flow = expense)
        var revenueTransactions = await transactionsQuery
            .Where(x => x.TransactionFlow == TransactionFlows.IN)
            .SumAsync(x => x.NetAmount, cancellationToken);

        var expenseTransactions = await transactionsQuery
            .Where(x => x.TransactionFlow == TransactionFlows.OUT)
            .SumAsync(x => Math.Abs(x.NetAmount), cancellationToken);

        var netRevenue = revenueTransactions - expenseTransactions;
        var periodDays = (endDate.Date - startDate.Date).Days;

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
}
