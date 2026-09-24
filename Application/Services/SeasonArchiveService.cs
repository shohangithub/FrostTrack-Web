using System.Collections.Concurrent;
using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Application.Services.Common;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class SeasonArchiveService : ISeasonArchiveService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<DeliveryChallan, Guid> _challanRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<Customer, int> _customerRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly IRepository<SeasonArchiveLog, Guid> _archiveLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBalanceCalculatorService _balanceCalculatorService;
    private readonly IUserContextService _userContextService;
    private readonly ITenantProvider _tenantProvider;

    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _archiveLocks = new();
    private static SemaphoreSlim GetLock(Guid tenantId) =>
        _archiveLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));

    public SeasonArchiveService(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<DeliveryChallan, Guid> challanRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<Customer, int> customerRepository,
        IRepository<Bank, int> bankRepository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        IRepository<SeasonArchiveLog, Guid> archiveLogRepository,
        IUnitOfWork unitOfWork,
        IBalanceCalculatorService balanceCalculatorService,
        IUserContextService userContextService,
        ITenantProvider tenantProvider)
    {
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _challanRepository = challanRepository;
        _transactionRepository = transactionRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _customerRepository = customerRepository;
        _bankRepository = bankRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _archiveLogRepository = archiveLogRepository;
        _unitOfWork = unitOfWork;
        _balanceCalculatorService = balanceCalculatorService;
        _userContextService = userContextService;
        _tenantProvider = tenantProvider;
    }

    public async Task<SeasonArchivePreviewResponse> GetArchivePreviewAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default)
    {
        var cutoffUtc = cutoffDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

        // 1. Bookings analysis
        var activeBookings = await _bookingRepository.Query()
            .Include(b => b.BookingDetails)
            .Where(b => !b.IsDeleted && !b.IsArchived && b.BookingDate <= cutoffUtc)
            .ToListAsync(cancellationToken);

        var activeBookingIds = activeBookings.Select(b => b.Id).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => !d.IsDeleted && activeBookingIds.Contains(d.BookingId) && d.DeliveryDate <= cutoffUtc)
            .ToListAsync(cancellationToken);

        var deliveriesByBooking = deliveries
            .GroupBy(d => d.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int completedBookingsCount = 0;
        int openBookingsCount = 0;
        decimal totalRemainingStock = 0m;

        foreach (var booking in activeBookings)
        {
            var bookedQty = booking.BookingDetails
                .Where(d => !d.IsDeleted)
                .Sum(d => d.BookingQuantity);

            var deliveredQty = deliveriesByBooking.TryGetValue(booking.Id, out var bDels)
                ? bDels.SelectMany(d => d.DeliveryDetails).Sum(dd => dd.DeliveryQuantity)
                : 0f;

            var remaining = bookedQty - deliveredQty;
            if (remaining <= 0.0001f)
            {
                completedBookingsCount++;
            }
            else
            {
                openBookingsCount++;
                totalRemainingStock += (decimal)remaining;
            }
        }

        // 2. Customers due analysis
        var customers = await _customerRepository.Query()
            .Where(c => !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var allPayments = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted && !t.IsArchived
                        && t.TransactionDate <= cutoffUtc
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT)
            .ToListAsync(cancellationToken);

        decimal totalCustomerNetDue = 0m;
        foreach (var customer in customers)
        {
            var custBookings = activeBookings.Where(b => b.CustomerId == customer.Id).ToList();
            var custBookingIds = custBookings.Select(b => b.Id).ToList();

            decimal totalBookingCharges = 0m;
            decimal totalDeliveryCharges = 0m;
            decimal totalRecurringCharges = 0m;

            foreach (var b in custBookings)
            {
                var activeDetails = b.BookingDetails.Where(d => !d.IsDeleted).ToList();
                var bDels = deliveriesByBooking.TryGetValue(b.Id, out var dl) ? dl : new List<Delivery>();

                var (_, _, _, pendingRecurring) =
                    BookingDueCalculator.CalculateBookingAccruedDetails(b, activeDetails, bDels, cutoffUtc);

                totalBookingCharges += activeDetails.Sum(d => d.LabourCharge);
                var bDeliveryRent = bDels.Sum(d => d.DeliveryDetails.Sum(dd => dd.ChargeAmount));
                var bDeliveryLabour = bDels.Sum(d => d.DeliveryDetails.Sum(dd => dd.LabourCharge));
                var bDeliveryAdj = bDels.Sum(d => d.AdjustmentValue);
                totalDeliveryCharges += (bDeliveryRent + bDeliveryLabour + bDeliveryAdj);
                totalRecurringCharges += pendingRecurring;
            }

            var custPayments = allPayments.Where(p => p.CustomerId == customer.Id || (p.BookingId.HasValue && custBookingIds.Contains(p.BookingId.Value))).Sum(p => p.Amount);
            var accrued = customer.OpeningBalance + totalBookingCharges + totalDeliveryCharges + totalRecurringCharges;
            var netDue = accrued - custPayments;
            totalCustomerNetDue += netDue;
        }

        // 3. Banks balance analysis
        var banks = await _bankRepository.Query()
            .Where(b => b.IsActive && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        var bankTxs = await _bankTransactionRepository.Query()
            .Where(bt => bt.IsActive && !bt.IsDeleted && !bt.IsArchived && bt.TransactionDate <= cutoffUtc)
            .ToListAsync(cancellationToken);

        var bankSummaries = new List<BankBalanceSummaryItem>();
        foreach (var bank in banks)
        {
            var delta = bankTxs
                .Where(bt => bt.BankId == bank.Id)
                .Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount);

            bankSummaries.Add(new BankBalanceSummaryItem
            {
                BankId = bank.Id,
                BankName = bank.BankName,
                AccountNumber = bank.AccountNumber,
                ClosingBalance = bank.OpeningBalance + delta
            });
        }

        // 4. Cash in hand analysis
        var cashInHand = await _balanceCalculatorService.GetCashOpeningBalanceAsync(cutoffUtc, cutoffUtc.AddDays(1), cancellationToken);

        // 5. Counts of items to archive
        var deliveriesToArchive = await _deliveryRepository.Query()
            .CountAsync(d => !d.IsDeleted && !d.IsArchived && d.DeliveryDate <= cutoffUtc, cancellationToken);

        var challansToArchive = await _challanRepository.Query()
            .CountAsync(c => !c.IsDeleted && !c.IsArchived && c.ChallanDate <= cutoffUtc, cancellationToken);

        var transactionsToArchive = await _transactionRepository.Query()
            .CountAsync(t => !t.IsDeleted && !t.IsArchived && t.TransactionDate <= cutoffUtc, cancellationToken);

        return new SeasonArchivePreviewResponse
        {
            ProposedCutoffDate = cutoffDate,
            CompletedBookingsCount = completedBookingsCount,
            OpenBookingsToCarryForwardCount = openBookingsCount,
            TotalRemainingStockBags = totalRemainingStock,
            TotalCustomersCount = customers.Count,
            TotalCustomerNetDue = totalCustomerNetDue,
            CurrentCashInHand = cashInHand,
            BankBalances = bankSummaries,
            DeliveriesToArchiveCount = deliveriesToArchive,
            ChallansToArchiveCount = challansToArchive,
            TransactionsToArchiveCount = transactionsToArchive
        };
    }

    public async Task<SeasonArchiveResultResponse> ExecuteArchiveAsync(
        ExecuteSeasonArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SeasonTitle))
            throw new ArgumentException("Season title is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmationCode) ||
            !request.ConfirmationCode.Trim().Equals("CONFIRM-ARCHIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid confirmation code. Please type CONFIRM-ARCHIVE to proceed.");
        }

        var tenantId = _tenantProvider.GetTenantId();
        var currentUser = _userContextService.GetCurrentUser();
        var sem = GetLock(tenantId);

        if (!await sem.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("A season archive operation is already in progress for this tenant.");

        try
        {
            var cutoffUtc = request.CutoffDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            var archiveTimestamp = DateTime.UtcNow;
            var batchCode = $"ARC-{request.CutoffDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Process Bookings: Archive completed bookings, keep open bookings active
            var activeBookings = await _bookingRepository.Query()
                .Include(b => b.BookingDetails)
                .Where(b => !b.IsDeleted && !b.IsArchived && b.BookingDate <= cutoffUtc)
                .ToListAsync(cancellationToken);

            var activeBookingIds = activeBookings.Select(b => b.Id).ToList();

            var deliveries = await _deliveryRepository.Query()
                .Include(d => d.DeliveryDetails)
                .Where(d => !d.IsDeleted && activeBookingIds.Contains(d.BookingId) && d.DeliveryDate <= cutoffUtc)
                .ToListAsync(cancellationToken);

            var deliveriesByBooking = deliveries
                .GroupBy(d => d.BookingId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int archivedBookingsCount = 0;
            int carriedForwardBookingsCount = 0;
            decimal totalCarriedStock = 0m;

            foreach (var booking in activeBookings)
            {
                var bookedQty = booking.BookingDetails
                    .Where(d => !d.IsDeleted)
                    .Sum(d => d.BookingQuantity);

                var deliveredQty = deliveriesByBooking.TryGetValue(booking.Id, out var bDels)
                    ? bDels.SelectMany(d => d.DeliveryDetails).Sum(dd => dd.DeliveryQuantity)
                    : 0f;

                var remaining = bookedQty - deliveredQty;
                if (remaining <= 0.0001f)
                {
                    booking.IsArchived = true;
                    booking.ArchivedAt = archiveTimestamp;
                    booking.ArchivedById = currentUser.Id;
                    await _bookingRepository.UpdateAsync(booking, cancellationToken);
                    archivedBookingsCount++;
                }
                else
                {
                    carriedForwardBookingsCount++;
                    totalCarriedStock += (decimal)remaining;
                }
            }

            // 2. Archive all Deliveries and Challans dated <= cutoffUtc
            var allDeliveriesToArchive = await _deliveryRepository.Query()
                .Where(d => !d.IsDeleted && !d.IsArchived && d.DeliveryDate <= cutoffUtc)
                .ToListAsync(cancellationToken);

            foreach (var del in allDeliveriesToArchive)
            {
                del.IsArchived = true;
                del.ArchivedAt = archiveTimestamp;
                del.ArchivedById = currentUser.Id;
                await _deliveryRepository.UpdateAsync(del, cancellationToken);
            }

            var allChallansToArchive = await _challanRepository.Query()
                .Where(c => !c.IsDeleted && !c.IsArchived && c.ChallanDate <= cutoffUtc)
                .ToListAsync(cancellationToken);

            foreach (var ch in allChallansToArchive)
            {
                ch.IsArchived = true;
                ch.ArchivedAt = archiveTimestamp;
                ch.ArchivedById = currentUser.Id;
                await _challanRepository.UpdateAsync(ch, cancellationToken);
            }

            // 3. Process Customers: Calculate net due and set as new OpeningBalance
            var customers = await _customerRepository.Query()
                .Where(c => !c.IsDeleted)
                .ToListAsync(cancellationToken);

            var allPayments = await _transactionRepository.Query()
                .Include(t => t.TransactionHead)
                .Where(t => !t.IsDeleted && !t.IsArchived
                            && t.TransactionDate <= cutoffUtc
                            && t.TransactionHead != null
                            && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                            && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT)
                .ToListAsync(cancellationToken);

            decimal totalCarriedCustomerDue = 0m;
            foreach (var customer in customers)
            {
                var custBookings = activeBookings.Where(b => b.CustomerId == customer.Id).ToList();
                var custBookingIds = custBookings.Select(b => b.Id).ToList();

                decimal totalBookingCharges = 0m;
                decimal totalDeliveryCharges = 0m;
                decimal totalRecurringCharges = 0m;

                foreach (var b in custBookings)
                {
                    var activeDetails = b.BookingDetails.Where(d => !d.IsDeleted).ToList();
                    var bDels = deliveriesByBooking.TryGetValue(b.Id, out var dl) ? dl : new List<Delivery>();

                    var (_, _, _, pendingRecurring) =
                        BookingDueCalculator.CalculateBookingAccruedDetails(b, activeDetails, bDels, cutoffUtc);

                    totalBookingCharges += activeDetails.Sum(d => d.LabourCharge);
                    var bDeliveryRent = bDels.Sum(d => d.DeliveryDetails.Sum(dd => dd.ChargeAmount));
                    var bDeliveryLabour = bDels.Sum(d => d.DeliveryDetails.Sum(dd => dd.LabourCharge));
                    var bDeliveryAdj = bDels.Sum(d => d.AdjustmentValue);
                    totalDeliveryCharges += (bDeliveryRent + bDeliveryLabour + bDeliveryAdj);
                    totalRecurringCharges += pendingRecurring;
                }

                var custPayments = allPayments.Where(p => p.CustomerId == customer.Id || (p.BookingId.HasValue && custBookingIds.Contains(p.BookingId.Value))).Sum(p => p.Amount);
                var accrued = customer.OpeningBalance + totalBookingCharges + totalDeliveryCharges + totalRecurringCharges;
                var netDue = accrued - custPayments;

                // Carry forward net balance as the customer's OpeningBalance for the new season
                customer.OpeningBalance = netDue;
                customer.LastUpdatedTime = archiveTimestamp;
                customer.LastUpdatedById = currentUser.Id;
                await _customerRepository.UpdateAsync(customer, cancellationToken);

                totalCarriedCustomerDue += netDue;
            }

            // 4. Process Banks: Carry forward closing balance as new OpeningBalance and archive bank transactions
            var banks = await _bankRepository.Query()
                .Where(b => b.IsActive && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var bankTxs = await _bankTransactionRepository.Query()
                .Where(bt => bt.IsActive && !bt.IsDeleted && !bt.IsArchived && bt.TransactionDate <= cutoffUtc)
                .ToListAsync(cancellationToken);

            decimal totalCarriedBankBalance = 0m;
            foreach (var bank in banks)
            {
                var delta = bankTxs
                    .Where(bt => bt.BankId == bank.Id)
                    .Sum(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount);

                var newOpeningBalance = bank.OpeningBalance + delta;
                bank.OpeningBalance = newOpeningBalance;
                bank.LastUpdatedTime = archiveTimestamp;
                bank.LastUpdatedById = currentUser.Id;
                await _bankRepository.UpdateAsync(bank, cancellationToken);

                totalCarriedBankBalance += newOpeningBalance;
            }

            foreach (var bt in bankTxs)
            {
                bt.IsArchived = true;
                bt.ArchivedAt = archiveTimestamp;
                bt.ArchivedById = currentUser.Id;
                await _bankTransactionRepository.UpdateAsync(bt, cancellationToken);
            }

            // 5. Process Cash in Hand: Calculate closing cash and carry forward
            var closingCash = await _balanceCalculatorService.GetCashOpeningBalanceAsync(cutoffUtc, cutoffUtc.AddDays(1), cancellationToken);

            // Archive all prior transactions <= cutoffUtc
            var transactionsToArchive = await _transactionRepository.Query()
                .Where(t => !t.IsDeleted && !t.IsArchived && t.TransactionDate <= cutoffUtc)
                .ToListAsync(cancellationToken);

            foreach (var tx in transactionsToArchive)
            {
                tx.IsArchived = true;
                tx.ArchivedAt = archiveTimestamp;
                tx.ArchivedById = currentUser.Id;
                await _transactionRepository.UpdateAsync(tx, cancellationToken);
            }

            // If cash in hand > 0 (or non-zero), create an OPENING_BALANCE transaction on the new season day 1
            if (closingCash != 0)
            {
                var openingHead = await _transactionHeadRepository.Query()
                    .FirstOrDefaultAsync(h => h.UsageFor == UsageFor.OPENING_BALANCE && h.Type == TransactionHeadTypes.CREDIT, cancellationToken);

                if (openingHead != null)
                {
                    var newSeasonStart = request.CutoffDate.Date.AddDays(1).ToUniversalTime();
                    var openCashTx = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        TransactionCode = $"OB-{request.CutoffDate.AddDays(1):yyyyMMdd}-001",
                        TransactionHeadId = openingHead.Id,
                        BranchId = currentUser.BranchId > 0 ? currentUser.BranchId : 1,
                        TransactionDate = newSeasonStart,
                        Amount = Math.Abs(closingCash),
                        NetAmount = Math.Abs(closingCash),
                        PaymentMethod = PaymentMethods.CASH,
                        Description = $"Season Opening Cash Balance carried forward from {request.SeasonTitle} ({batchCode})",
                        Note = $"Batch Code: {batchCode}",
                        CreatedById = currentUser.Id,
                        CreatedTime = archiveTimestamp,
                        IsArchived = false,
                        IsDeleted = false
                    };
                    await _transactionRepository.AddAsync(openCashTx, cancellationToken);
                }
            }

            // 6. Write SeasonArchiveLog audit record
            var executedUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            if (string.IsNullOrEmpty(executedUserName)) executedUserName = currentUser.Email ?? "Administrator";

            var archiveLog = new SeasonArchiveLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BatchCode = batchCode,
                SeasonTitle = request.SeasonTitle,
                CutoffDate = request.CutoffDate,
                TotalArchivedBookings = archivedBookingsCount,
                TotalCarriedForwardBookings = carriedForwardBookingsCount,
                TotalCarriedForwardStock = totalCarriedStock,
                TotalCarriedForwardCustomerDue = totalCarriedCustomerDue,
                TotalCarriedForwardCashBalance = closingCash,
                TotalCarriedForwardBankBalance = totalCarriedBankBalance,
                TotalArchivedDeliveries = allDeliveriesToArchive.Count,
                TotalArchivedTransactions = transactionsToArchive.Count,
                TotalArchivedChallans = allChallansToArchive.Count,
                ExecutedById = currentUser.Id,
                ExecutedByName = executedUserName,
                ExecutedAt = archiveTimestamp,
                Notes = request.Notes
            };

            await _archiveLogRepository.AddAsync(archiveLog, cancellationToken);

            // Commit the entire atomic unit of work
            await transaction.CommitAsync(cancellationToken);

            return new SeasonArchiveResultResponse
            {
                ArchiveBatchId = archiveLog.Id,
                BatchCode = batchCode,
                SeasonTitle = request.SeasonTitle,
                CutoffDate = request.CutoffDate,
                ArchivedBookingsCount = archivedBookingsCount,
                CarriedForwardBookingsCount = carriedForwardBookingsCount,
                CarriedForwardStockBags = totalCarriedStock,
                CarriedForwardCustomerDue = totalCarriedCustomerDue,
                CarriedForwardCashBalance = closingCash,
                CarriedForwardBankBalance = totalCarriedBankBalance,
                ArchivedDeliveriesCount = allDeliveriesToArchive.Count,
                ArchivedTransactionsCount = transactionsToArchive.Count,
                ExecutedAt = archiveTimestamp,
                Message = $"Season '{request.SeasonTitle}' archived successfully with batch code {batchCode}. All master data and carry-over rest balances are ready for the new season!"
            };
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<IEnumerable<SeasonArchiveHistoryResponse>> GetArchiveHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var logs = await _archiveLogRepository.Query()
            .OrderByDescending(l => l.ExecutedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new SeasonArchiveHistoryResponse
        {
            Id = l.Id,
            BatchCode = l.BatchCode,
            SeasonTitle = l.SeasonTitle,
            CutoffDate = l.CutoffDate,
            TotalArchivedBookings = l.TotalArchivedBookings,
            TotalCarriedForwardBookings = l.TotalCarriedForwardBookings,
            TotalCarriedForwardStock = l.TotalCarriedForwardStock,
            TotalCarriedForwardCustomerDue = l.TotalCarriedForwardCustomerDue,
            TotalCarriedForwardCashBalance = l.TotalCarriedForwardCashBalance,
            TotalCarriedForwardBankBalance = l.TotalCarriedForwardBankBalance,
            TotalArchivedDeliveries = l.TotalArchivedDeliveries,
            TotalArchivedTransactions = l.TotalArchivedTransactions,
            ExecutedByName = l.ExecutedByName,
            ExecutedAt = l.ExecutedAt,
            Notes = l.Notes
        });
    }
}
