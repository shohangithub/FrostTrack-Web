namespace Application.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IRepository<Delivery, Guid> _repository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<BookingDetail, Guid> _bookingDetailRepository;
    private readonly IRepository<DeliveryDetail, Guid> _detailRepository;
    private readonly IRepository<UnitConversion, int> _unitConversionRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IRepository<BankTransaction, long> _bankTransactionRepository;
    private readonly IRepository<DeliveryChallanItem, Guid> _challanItemRepository;
    private readonly IRepository<DeliveryChallan, Guid> _challanRepository;
    private readonly IEditWindowGuard _editWindowGuard;

    private readonly ITransactionService _transactionService;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryService(
        IRepository<Delivery, Guid> repository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<BookingDetail, Guid> bookingDetailRepository,
        IRepository<DeliveryDetail, Guid> detailRepository,
        IRepository<UnitConversion, int> unitConversionRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        IRepository<Bank, int> bankRepository,
        IRepository<BankTransaction, long> bankTransactionRepository,
        IRepository<DeliveryChallanItem, Guid> challanItemRepository,
        IRepository<DeliveryChallan, Guid> challanRepository,
        IEditWindowGuard editWindowGuard,

        ITransactionService transactionService,
        ICodeGenerationService codeGenerationService,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _bookingRepository = bookingRepository;
        _bookingDetailRepository = bookingDetailRepository;
        _detailRepository = detailRepository;
        _unitConversionRepository = unitConversionRepository;
        _transactionRepository = transactionRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _bankRepository = bankRepository;
        _bankTransactionRepository = bankTransactionRepository;
        _challanItemRepository = challanItemRepository;
        _challanRepository = challanRepository;
        _editWindowGuard = editWindowGuard;

        _transactionService = transactionService;
        _codeGenerationService = codeGenerationService;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _unitOfWork = unitOfWork;
    }

    public async Task<DeliveryResponse> CreateAsync(CreateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        // Validate stock availability
        await ValidateStockAvailability(request);

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = request.Adapt<Delivery>();
            entity.BranchId = _currentUser.BranchId;
            _defaultValueInjector.InjectCreatingAudit<Delivery, Guid>(entity);

            if (entity.DeliveryDetails != null && entity.DeliveryDetails.Any())
            {
                foreach (var detail in entity.DeliveryDetails)
                {
                    detail.BaseQuantity = await CalculateBaseQuantityAsync(detail.DeliveryUnitId, detail.DeliveryQuantity, cancellationToken);
                }
                _defaultValueInjector.InjectCreatingAudit<DeliveryDetail, Guid>(entity.DeliveryDetails.ToList());
            }

            var details = entity.DeliveryDetails?.ToList() ?? [];
            var deliveryGrandTotal = CalculateGrandTotal(details);
            var paymentAmount = request.TransactionAmount.HasValue && request.TransactionAmount.Value > 0
                ? request.TransactionAmount.Value
                : 0m;

            if (paymentAmount > deliveryGrandTotal + 0.01m)
                throw new Exception($"Collection amount (৳{paymentAmount:N2}) cannot exceed delivery total (৳{deliveryGrandTotal:N2})");

            ApplyCollection(entity, details, paymentAmount, deliveryGrandTotal);
            entity.TransactionId = null;

            await _repository.AddAsync(entity, CancellationToken.None);

            if (paymentAmount > 0)
            {
                var customerId = await _bookingRepository.Query()
                    .Where(b => b.Id == request.BookingId)
                    .Select(b => (int?)b.CustomerId)
                    .FirstOrDefaultAsync(cancellationToken);

                await SyncPaymentTransactionAsync(entity, customerId, paymentAmount,
                    request.PaymentMethod, request.BankId, request.PaymentReference, request.TransactionNotes, cancellationToken);

                await _repository.UpdateAsync(entity, CancellationToken.None);
            }

            await dbTransaction.CommitAsync(cancellationToken);
            return await GetByIdAsync(entity.Id);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DeliveryResponse> UpdateAsync(Guid id, UpdateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.Query()
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (existing == null)
            throw new Exception("Product delivery not found");

        await _editWindowGuard.EnsureEditableAsync(existing.DeliveryDate, "delivery", cancellationToken);
        await _editWindowGuard.EnsureEditableAsync(request.DeliveryDate, "delivery", cancellationToken);
        await EnsureNoExternalPaymentsAsync(existing, "modified", cancellationToken);

        // Validate stock availability (excluding this delivery's current quantities)
        await ValidateStockAvailability(request, id);

        // Build the new detail rows (base quantity is always recalculated server-side)
        var newDetails = new List<DeliveryDetail>();
        foreach (var d in request.DeliveryDetails ?? [])
        {
            var newDetail = new DeliveryDetail
            {
                DeliveryId = id,
                BookingDetailId = d.BookingDetailId,
                DeliveryUnitId = d.DeliveryUnitId,
                DeliveryQuantity = d.DeliveryQuantity,
                BaseQuantity = await CalculateBaseQuantityAsync(d.DeliveryUnitId, d.DeliveryQuantity, cancellationToken),
                ChargeAmount = d.ChargeAmount,
                LabourCharge = d.LabourCharge,
                AdjustmentValue = d.AdjustmentValue,
                BillingCycles = d.BillingCycles
            };
            newDetails.Add(newDetail);
        }
        _defaultValueInjector.InjectCreatingAudit<DeliveryDetail, Guid>(newDetails);

        // Resolve payment: null amount keeps what was collected at delivery time
        var existingTx = await FindDeliveryPaymentTransactionAsync(existing, cancellationToken);
        var deliveryGrandTotal = CalculateGrandTotal(newDetails);
        var paymentAmount = request.TransactionAmount.HasValue
            ? Math.Max(request.TransactionAmount.Value, 0m)
            : existing.CollectedAmount;

        if (paymentAmount > deliveryGrandTotal + 0.01m)
            throw new Exception($"Collection amount (৳{paymentAmount:N2}) cannot exceed delivery total (৳{deliveryGrandTotal:N2})");

        var paymentMethod = request.PaymentMethod ?? existingTx?.PaymentMethod;
        var bankId = request.PaymentMethod != null ? request.BankId : existingTx?.BankId;
        var paymentReference = request.PaymentMethod != null ? request.PaymentReference : existingTx?.PaymentReference;
        var paymentNotes = request.TransactionNotes ?? existingTx?.Note;

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = await _repository.UpdatableQuery(x => x.Id == id).FirstAsync(cancellationToken);
            entity.DeliveryNumber = request.DeliveryNumber;
            entity.DeliveryDate = request.DeliveryDate;
            entity.Notes = request.Notes;
            entity.ChargeAmount = request.ChargeAmount;
            entity.AdjustmentValue = request.AdjustmentValue;
            _defaultValueInjector.InjectUpdatingAudit<Delivery, Guid>(entity);

            ApplyCollection(entity, newDetails, paymentAmount, deliveryGrandTotal);

            // Replace detail rows
            await _detailRepository.DeletableQuery(dd => dd.DeliveryId == id).ExecuteDeleteAsync(cancellationToken);
            foreach (var detail in newDetails)
            {
                await _detailRepository.AddAsync(detail, cancellationToken);
            }

            await SyncPaymentTransactionAsync(entity, existing.Booking?.CustomerId, paymentAmount,
                paymentMethod, bankId, paymentReference, paymentNotes, cancellationToken);

            await _repository.UpdateAsync(entity, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Permanent delete is usually invoked from the "deleted" tab, so look past the soft-delete filter
        var delivery = await _repository.UnfilteredQuery()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantId);

        if (delivery == null)
            throw new Exception("Product delivery not found");

        await _editWindowGuard.EnsureEditableAsync(delivery.DeliveryDate, "delivery");
        await EnsureNoExternalPaymentsAsync(delivery, "deleted");

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var txIds = await GetLinkedTransactionIdsAsync(delivery);

            // Remove the delivery from any challan it was printed on
            var challanIds = await _challanItemRepository.Query()
                .Where(ci => ci.DeliveryId == id)
                .Select(ci => ci.DeliveryChallanId)
                .Distinct()
                .ToListAsync();

            await _challanItemRepository.DeletableQuery(ci => ci.DeliveryId == id).ExecuteDeleteAsync();

            // A challan left with no deliveries is moved to the Deleted tab (header kept for the record)
            if (challanIds.Count > 0)
            {
                DateTime? now = DateTime.UtcNow;
                int? userId = _currentUser.Id;
                await _challanRepository.UnfilteredQuery()
                    .Where(c => challanIds.Contains(c.Id) && !c.IsDeleted && !c.ChallanItems.Any())
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.DeletedAt, now)
                        .SetProperty(x => x.DeletedById, userId));
            }

            // Detach the payment transaction before removing it (Delivery.TransactionId is a restrict FK)
            await _repository.UnfilteredQuery()
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TransactionId, (Guid?)null));

            if (txIds.Count > 0)
            {
                await _bankTransactionRepository.UnfilteredQuery()
                    .Where(bt => bt.TransactionId.HasValue && txIds.Contains(bt.TransactionId.Value))
                    .ExecuteDeleteAsync();

                await _transactionRepository.UnfilteredQuery()
                    .Where(t => txIds.Contains(t.Id))
                    .ExecuteDeleteAsync();
            }

            await _detailRepository.UnfilteredQuery().Where(dd => dd.DeliveryId == id).ExecuteDeleteAsync();
            var result = await _repository.UnfilteredQuery().Where(x => x.Id == id).ExecuteDeleteAsync();

            await dbTransaction.CommitAsync();
            return result > 0;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> BatchDeleteAsync(Guid[] ids)
    {
        var deleted = 0;
        foreach (var id in ids.Distinct())
        {
            if (await DeleteAsync(id)) deleted++;
        }
        return deleted > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Product delivery not found");

        await _editWindowGuard.EnsureEditableAsync(entity.DeliveryDate, "delivery", cancellationToken);
        await EnsureNoExternalPaymentsAsync(entity, "deleted", cancellationToken);

        DateTime? now = DateTime.UtcNow;
        int? userId = _currentUser.Id;

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var txIds = await GetLinkedTransactionIdsAsync(entity, cancellationToken);

            await _repository.UnfilteredQuery()
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, now)
                    .SetProperty(x => x.DeletedById, userId), cancellationToken);

            // Details must be flagged too, otherwise they keep consuming booking stock
            await _detailRepository.UnfilteredQuery()
                .Where(dd => dd.DeliveryId == id && !dd.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, now)
                    .SetProperty(x => x.DeletedById, userId), cancellationToken);

            if (txIds.Count > 0)
            {
                await _transactionRepository.UnfilteredQuery()
                    .Where(t => txIds.Contains(t.Id) && !t.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.DeletedAt, now)
                        .SetProperty(x => x.DeletedById, userId), cancellationToken);

                await _bankTransactionRepository.UnfilteredQuery()
                    .Where(bt => bt.TransactionId.HasValue && txIds.Contains(bt.TransactionId.Value) && !bt.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.DeletedAt, now)
                        .SetProperty(x => x.DeletedById, userId), cancellationToken);
            }

            await dbTransaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery()
            .Include(x => x.DeliveryDetails)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantId, cancellationToken);
        if (entity == null) throw new Exception("Product delivery not found");

        // Restoring must not over-deliver a booking that has since been delivered again
        foreach (var detail in entity.DeliveryDetails)
        {
            var bookingBaseQty = await _bookingDetailRepository.Query()
                .Where(bd => bd.Id == detail.BookingDetailId)
                .Select(bd => bd.BaseQuantity)
                .FirstOrDefaultAsync(cancellationToken);

            var deliveredBaseQty = await ActiveDeliveryDetails()
                .Where(dd => dd.BookingDetailId == detail.BookingDetailId && dd.DeliveryId != id)
                .SumAsync(dd => dd.BaseQuantity, cancellationToken);

            if (deliveredBaseQty + detail.BaseQuantity > bookingBaseQty)
                throw new Exception("Cannot restore this delivery: the booking stock has already been delivered by another delivery.");
        }

        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var txIds = await GetLinkedTransactionIdsAsync(entity, cancellationToken);

            await _repository.UnfilteredQuery()
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, false)
                    .SetProperty(x => x.DeletedAt, (DateTime?)null)
                    .SetProperty(x => x.DeletedById, (int?)null), cancellationToken);

            await _detailRepository.UnfilteredQuery()
                .Where(dd => dd.DeliveryId == id && dd.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsDeleted, false)
                    .SetProperty(x => x.DeletedAt, (DateTime?)null)
                    .SetProperty(x => x.DeletedById, (int?)null), cancellationToken);

            if (txIds.Count > 0)
            {
                await _transactionRepository.UnfilteredQuery()
                    .Where(t => txIds.Contains(t.Id) && t.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, false)
                        .SetProperty(x => x.DeletedAt, (DateTime?)null)
                        .SetProperty(x => x.DeletedById, (int?)null), cancellationToken);

                await _bankTransactionRepository.UnfilteredQuery()
                    .Where(bt => bt.TransactionId.HasValue && txIds.Contains(bt.TransactionId.Value) && bt.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, false)
                        .SetProperty(x => x.DeletedAt, (DateTime?)null)
                        .SetProperty(x => x.DeletedById, (int?)null), cancellationToken);
            }

            await dbTransaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    #region Delivery payment / cascade helpers

    private IQueryable<DeliveryDetail> ActiveDeliveryDetails()
        => _detailRepository.Query().Where(dd => !dd.IsDeleted && !dd.Delivery!.IsDeleted);

    private async Task<decimal> CalculateBaseQuantityAsync(int deliveryUnitId, float deliveryQuantity, CancellationToken cancellationToken)
    {
        var unitConversion = await _unitConversionRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == deliveryUnitId, cancellationToken);

        return unitConversion != null
            ? (decimal)(deliveryQuantity * unitConversion.ConversionValue)
            : (decimal)deliveryQuantity;
    }

    private static decimal CalculateGrandTotal(IEnumerable<DeliveryDetail> details)
        => details.Sum(d => d.ChargeAmount + d.AdjustmentValue + d.LabourCharge);

    /// <summary>
    /// Sets CollectedAmount / PaymentStatus on the delivery and distributes the collected amount across its details.
    /// </summary>
    private static void ApplyCollection(Delivery entity, IList<DeliveryDetail> details, decimal paymentAmount, decimal grandTotal)
    {
        entity.CollectedAmount = paymentAmount;

        if (paymentAmount > 0 && paymentAmount >= grandTotal - 0.01m)
        {
            entity.PaymentStatus = PaymentStatuses.PAID;
            entity.PaymentDate = entity.DeliveryDate;
        }
        else
        {
            entity.PaymentStatus = PaymentStatuses.UNPAID;
            entity.PaymentDate = null;
        }

        var remaining = paymentAmount;
        for (int i = 0; i < details.Count; i++)
        {
            var detail = details[i];
            var detailDue = detail.ChargeAmount + detail.LabourCharge + detail.AdjustmentValue;
            var allocated = i == details.Count - 1 ? remaining : Math.Min(remaining, Math.Max(detailDue, 0m));
            detail.CollectedAmount = allocated;
            remaining -= allocated;
        }
    }

    /// <summary>
    /// Creates, updates or removes the delivery-time CUSTOMER_PAYMENT transaction and its bank deposit
    /// so they match <paramref name="paymentAmount"/>. Sets entity.TransactionId; the caller saves the delivery.
    /// </summary>
    private async Task SyncPaymentTransactionAsync(
        Delivery entity,
        int? customerId,
        decimal paymentAmount,
        string? paymentMethod,
        int? bankId,
        string? paymentReference,
        string? notes,
        CancellationToken cancellationToken)
    {
        var method = string.IsNullOrWhiteSpace(paymentMethod) ? PaymentMethods.CASH : paymentMethod;
        var existingTx = await FindDeliveryPaymentTransactionAsync(entity, cancellationToken);

        if (paymentAmount <= 0)
        {
            if (existingTx != null)
            {
                entity.TransactionId = null;
                await _repository.UpdateAsync(entity, cancellationToken);

                var removedTxId = existingTx.Id;
                await _bankTransactionRepository.DeletableQuery(bt => bt.TransactionId == removedTxId).ExecuteDeleteAsync(cancellationToken);
                await _transactionRepository.DeletableQuery(t => t.Id == removedTxId).ExecuteDeleteAsync(cancellationToken);
            }
            entity.TransactionId = null;
            return;
        }

        string transactionCode;
        Guid transactionId;

        if (existingTx == null)
        {
            var transactionHead = await _transactionHeadRepository.Query()
                .FirstOrDefaultAsync(th => th.Type == TransactionHeadTypes.DEBIT && th.UsageFor == UsageFor.CUSTOMER_PAYMENT && th.IsActive, cancellationToken);

            if (transactionHead == null)
                throw new Exception("CUSTOMER_PAYMENT transaction head not found");

            transactionCode = await GenerateDeliveryTransactionCodeAsync(entity.DeliveryDate, cancellationToken);

            var chargeTransactionRequest = new TransactionRequest(
                Id: Guid.NewGuid(),
                TransactionCode: transactionCode,
                TransactionDate: entity.DeliveryDate,
                TransactionHeadId: transactionHead.Id,
                BranchId: entity.BranchId,
                CustomerId: customerId,
                BookingId: entity.BookingId,
                DeliveryId: entity.Id,
                Amount: paymentAmount,
                DiscountAmount: 0,
                AdjustmentValue: 0,
                NetAmount: paymentAmount,
                PaymentMethod: method,
                PaymentReference: paymentReference,
                BankId: bankId,
                Category: null,
                SubCategory: null,
                Description: $"Payment for Delivery {entity.DeliveryNumber}",
                Note: notes
            );

            var chargeTransaction = await _transactionService.AddAsync(chargeTransactionRequest, CancellationToken.None);
            transactionId = chargeTransaction.Id;
        }
        else
        {
            var existingTxId = existingTx.Id;
            var tx = await _transactionRepository.UpdatableQuery(t => t.Id == existingTxId).FirstAsync(cancellationToken);
            tx.TransactionDate = entity.DeliveryDate;
            tx.Amount = paymentAmount;
            tx.NetAmount = paymentAmount;
            tx.PaymentMethod = method;
            tx.PaymentReference = paymentReference;
            tx.BankId = bankId;
            tx.Note = notes;
            tx.DeliveryId = entity.Id;
            tx.Description = $"Payment for Delivery {entity.DeliveryNumber}";
            _defaultValueInjector.InjectUpdatingAudit<Transaction, Guid>(tx);
            await _transactionRepository.UpdateAsync(tx, cancellationToken);

            transactionCode = tx.TransactionCode;
            transactionId = tx.Id;
        }

        entity.TransactionId = transactionId;

        // Bank / cheque payments carry a matching bank deposit; cash payments must not
        var bankTx = await _bankTransactionRepository.UpdatableQuery(bt => bt.TransactionId == transactionId && !bt.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (method != PaymentMethods.CASH && bankId.HasValue)
        {
            var bank = await _bankRepository.GetByIdAsync(bankId.Value, cancellationToken);
            if (bank == null) return;

            var excludeBankTxId = bankTx?.Id ?? 0L;
            var depositBankId = bankId.Value;
            var otherMovements = await _bankTransactionRepository.Query()
                .Where(bt => bt.BankId == depositBankId && bt.IsActive && !bt.IsDeleted && bt.Id != excludeBankTxId)
                .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);
            var balanceAfter = bank.OpeningBalance + otherMovements + paymentAmount;
            var description = $"Delivery Payment - {entity.DeliveryNumber} ({method})";

            if (bankTx == null)
            {
                bankTx = new BankTransaction
                {
                    TransactionNumber = transactionCode,
                    TransactionDate = entity.DeliveryDate,
                    BankId = bankId.Value,
                    TransactionType = BankTransactionTypes.Deposit,
                    Amount = paymentAmount,
                    Reference = paymentReference,
                    Description = description,
                    BalanceAfter = balanceAfter,
                    SourceType = BankSourceTypes.DELIVERY,
                    TransactionId = transactionId,
                    BranchId = entity.BranchId,
                    IsActive = true
                };
                _defaultValueInjector.InjectCreatingAudit<BankTransaction, long>(bankTx);
                await _bankTransactionRepository.AddAsync(bankTx, cancellationToken);
            }
            else
            {
                bankTx.TransactionDate = entity.DeliveryDate;
                bankTx.BankId = bankId.Value;
                bankTx.Amount = paymentAmount;
                bankTx.Reference = paymentReference;
                bankTx.Description = description;
                bankTx.BalanceAfter = balanceAfter;
                _defaultValueInjector.InjectUpdatingAudit<BankTransaction, long>(bankTx);
                await _bankTransactionRepository.UpdateAsync(bankTx, cancellationToken);
            }
        }
        else if (bankTx != null)
        {
            await _bankTransactionRepository.DeletableQuery(bt => bt.TransactionId == transactionId).ExecuteDeleteAsync(cancellationToken);
        }
    }

    /// <summary>The CUSTOMER_PAYMENT transaction recorded at delivery time, if any.</summary>
    private async Task<Transaction?> FindDeliveryPaymentTransactionAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        if (delivery.TransactionId.HasValue)
        {
            var txId = delivery.TransactionId.Value;
            var tx = await _transactionRepository.Query().FirstOrDefaultAsync(t => t.Id == txId, cancellationToken);
            if (tx != null) return tx;
        }

        var deliveryId = delivery.Id;
        return await _transactionRepository.Query()
            .Where(t => t.DeliveryId == deliveryId)
            .OrderBy(t => t.CreatedTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>All transactions (including soft-deleted) that belong to this delivery.</summary>
    private async Task<List<Guid>> GetLinkedTransactionIdsAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        var deliveryId = delivery.Id;
        var mainTxId = delivery.TransactionId ?? Guid.Empty;
        return await _transactionRepository.UnfilteredQuery()
            .Where(t => t.TenantId == _tenantId && (t.DeliveryId == deliveryId || t.Id == mainTxId))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Refuses the operation when the delivery has payments other than the one recorded at delivery time
    /// (e.g. a later collection linked to this delivery). Those must be reversed first.
    /// </summary>
    private async Task EnsureNoExternalPaymentsAsync(Delivery delivery, string action, CancellationToken cancellationToken = default)
    {
        var mainTx = await FindDeliveryPaymentTransactionAsync(delivery, cancellationToken);
        var mainTxId = mainTx?.Id ?? Guid.Empty;
        var deliveryId = delivery.Id;

        var otherPayments = await _transactionRepository.Query()
            .Where(t => t.DeliveryId == deliveryId && t.Id != mainTxId)
            .Select(t => t.TransactionCode)
            .ToListAsync(cancellationToken);

        if (otherPayments.Count > 0)
            throw new Exception($"This delivery cannot be {action} because later payments are linked to it ({string.Join(", ", otherPayments)}). Delete those transactions first.");
    }

    private async Task<string> GenerateDeliveryTransactionCodeAsync(DateTime txDate, CancellationToken cancellationToken)
    {
        var datePart = txDate.ToString("yyMMdd");
        const string prefix = "DEL";

        var existingCodes = await _transactionRepository.UnfilteredQuery()
            .Where(x => x.TenantId == _tenantId && x.TransactionCode.StartsWith($"{prefix}-{datePart}-"))
            .Select(x => x.TransactionCode)
            .ToListAsync(cancellationToken);

        int maxSequence = 0;
        var seqPattern = new System.Text.RegularExpressions.Regex($@"^{System.Text.RegularExpressions.Regex.Escape(prefix)}-{datePart}-(\d+)");
        foreach (var code in existingCodes)
        {
            var match = seqPattern.Match(code);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seq) && seq > maxSequence)
                maxSequence = seq;
        }

        return CodeGenerator.GenerateTransactionCode(prefix, maxSequence + 1, txDate);
    }

    #endregion

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Product delivery not found");

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Product delivery not found");

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<DeliveryResponse> GetByIdAsync(Guid id)
    {
        var entity = await _repository.Query()
            .Include(x => x.Booking)
            .Include(x => x.Branch)
            .Include(x => x.DeliveryDetails)
                .ThenInclude(d => d.BookingDetail)
                    .ThenInclude(bd => bd!.Product)
            .Include(x => x.DeliveryDetails)
                .ThenInclude(d => d.DeliveryUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception("Product delivery not found");

        var response = entity.Adapt<DeliveryResponse>();

        // Delivery-time payment details for the edit form
        var paymentTx = await FindDeliveryPaymentTransactionAsync(entity);
        if (paymentTx != null)
        {
            response.TransactionId = paymentTx.Id;
            response.PaymentMethod = paymentTx.PaymentMethod;
            response.BankId = paymentTx.BankId;
            response.PaymentReference = paymentTx.PaymentReference;
            response.TransactionNotes = paymentTx.Note;
        }

        // Populate additional fields from booking details for edit functionality
        foreach (var detail in response.DeliveryDetails)
        {
            var deliveryDetail = entity.DeliveryDetails.FirstOrDefault(d => d.Id == detail.Id);
            if (deliveryDetail?.BookingDetail != null)
            {
                detail.BillingCycles = deliveryDetail.BillingCycles;
                detail.BookingRate = deliveryDetail.BookingDetail.BookingRate;
                detail.BillType = deliveryDetail.BookingDetail.BillType;
                detail.LabourCharge = deliveryDetail.LabourCharge;
            }
        }

        return response;
    }

    public async Task<PaginationResult<DeliveryResponse>> GetWithPaginationAsync(DeliveryPaginationQuery query)
    {
        var status = query.Status?.ToLowerInvariant() ?? "active";
        var baseQuery = status == "deleted"
            ? _repository.UnfilteredQuery().AsQueryable()
            : _repository.Query().AsQueryable();

        baseQuery = status switch
        {
            "archived" => baseQuery.Where(x => !x.IsDeleted && x.IsArchived),
            "deleted" => baseQuery.Where(x => x.IsDeleted && x.TenantId == _tenantId),
            _ => baseQuery.Where(x => !x.IsDeleted && !x.IsArchived)
        };

        // Filtering
        if (!string.IsNullOrEmpty(query.OpenText))
        {
            var text = query.OpenText;
            baseQuery = baseQuery.Where(x =>
                x.DeliveryNumber.Contains(text) ||
                x.Booking!.BookingNumber.Contains(text) ||
                x.Booking!.Customer!.CustomerName.Contains(text)
            );
        }

        // Ordering
        baseQuery = ApplyOrdering(baseQuery, query.OrderBy, query.IsAscending);

        // Projection WITHOUT Includes (optimized)
        var projectedQuery = baseQuery.Select(x => new DeliveryResponse
        {
            Id = x.Id,
            DeliveryNumber = x.DeliveryNumber,
            DeliveryDate = x.DeliveryDate,
            BookingId = x.BookingId,
            BookingNumber = x.Booking.BookingNumber,
            CustomerId = x.Booking.CustomerId,
            CustomerName = x.Booking.Customer.CustomerName,
            BranchId = x.BranchId,
            BranchName = x.Branch.Name,
            ChargeAmount = x.ChargeAmount,
            AdjustmentValue = x.AdjustmentValue,
            CollectedAmount = x.CollectedAmount,
            IsDeleted = x.IsDeleted,
            DeletedAt = x.DeletedAt,
            IsArchived = x.IsArchived,
            ArchivedAt = x.ArchivedAt,
            CreatedAt = x.CreatedTime,

            DeliveryDetails = x.DeliveryDetails.Select(d => new DeliveryDetailResponse
            {
                Id = d.Id,
                DeliveryId = d.DeliveryId,
                BookingDetailId = d.BookingDetailId,
                ProductId = d.BookingDetail.ProductId,
                ProductName = d.BookingDetail.Product.ProductName,
                DeliveryUnitId = d.DeliveryUnitId,
                DeliveryUnitName = d.DeliveryUnit.UnitName,
                DeliveryQuantity = d.DeliveryQuantity,
                BaseQuantity = d.BaseQuantity,
                ChargeAmount = d.ChargeAmount,
                LabourCharge = d.LabourCharge,
                AdjustmentValue = d.AdjustmentValue,
                CollectedAmount = d.CollectedAmount
            }).ToList()
        });

        return await PaginationResult<DeliveryResponse>.CreateAsync(
            projectedQuery,
            query.PageIndex,
            query.PageSize);
    }


    public async Task<string> GenerateDeliveryNumberAsync()
    {
        return await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "DEL",
            d => d.DeliveryNumber);
    }

    public async Task<List<CustomerStockResponse>> GetCustomerStockAsync(int customerId)
    {
        // Get all bookings for this customer with product details
        var bookings = await _bookingRepository.Query()
            .Where(b => b.CustomerId == customerId)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.BookingUnit)
            .ToListAsync();

        // Get all deliveries for bookings of this customer
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var deliveries = await _repository.Query()
            .Where(d => bookingIds.Contains(d.BookingId))
            .Include(d => d.DeliveryDetails)
            .ToListAsync();

        // Calculate stock per booking detail
        var stockDictionary = new Dictionary<Guid, CustomerStockResponse>();

        foreach (var booking in bookings)
        {
            foreach (var detail in booking.BookingDetails)
            {
                var key = detail.Id;
                if (!stockDictionary.ContainsKey(key))
                {
                    stockDictionary[key] = new CustomerStockResponse
                    {
                        CustomerId = customerId,
                        BookingDetailId = detail.Id,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName ?? "",
                        UnitId = detail.BookingUnitId,
                        UnitName = detail.BookingUnit?.UnitName ?? "",
                        AvailableStock = (decimal)detail.BookingQuantity,
                        BookingRate = detail.BookingRate
                    };
                }
            }
        }

        // Subtract delivered quantities
        foreach (var delivery in deliveries)
        {
            foreach (var detail in delivery.DeliveryDetails)
            {
                if (stockDictionary.ContainsKey(detail.BookingDetailId))
                {
                    stockDictionary[detail.BookingDetailId] = stockDictionary[detail.BookingDetailId] with
                    {
                        AvailableStock = stockDictionary[detail.BookingDetailId].AvailableStock - (decimal)detail.DeliveryQuantity
                    };
                }
            }
        }

        // Return only products with available stock > 0
        return stockDictionary.Values
            .Where(x => x.AvailableStock > 0)
            .ToList();
    }

    private async Task ValidateStockAvailability(CreateDeliveryRequest request, Guid? existingDeliveryId = null)
    {
        // Get the booking to find customer
        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null)
            throw new Exception("Booking not found");

        foreach (var detail in request.DeliveryDetails)
        {
            var bookingDetail = await _bookingDetailRepository.Query()
                .Include(bd => bd.BookingUnit)
                .FirstOrDefaultAsync(bd => bd.Id == detail.BookingDetailId);

            if (bookingDetail == null)
                throw new Exception($"Booking detail not found");

            // Calculate already delivered base quantity for this booking detail
            var deliveredBaseQty = await ActiveDeliveryDetails()
                .Where(dd => dd.BookingDetailId == detail.BookingDetailId && dd.DeliveryId != existingDeliveryId)
                .SumAsync(dd => dd.BaseQuantity);

            // Calculate booking base quantity
            var bookingBaseQty = bookingDetail.BaseQuantity;

            // Calculate remaining base quantity
            var remainingBaseQty = bookingBaseQty - deliveredBaseQty;

            // Get delivery unit conversion to calculate delivery base quantity
            var deliveryUnit = await _unitConversionRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == detail.DeliveryUnitId);

            var deliveryBaseQty = deliveryUnit != null
                ? (decimal)(detail.DeliveryQuantity * deliveryUnit.ConversionValue)
                : (decimal)detail.DeliveryQuantity;

            if (deliveryBaseQty > remainingBaseQty)
            {
                // Calculate available in delivery unit for user-friendly error message
                var availableInDeliveryUnit = deliveryUnit != null && deliveryUnit.ConversionValue > 0
                    ? remainingBaseQty / (decimal)deliveryUnit.ConversionValue
                    : remainingBaseQty;

                throw new Exception($"Insufficient stock for product. Available: {availableInDeliveryUnit:F2} (in selected unit), Requested: {detail.DeliveryQuantity}");
            }
        }
    }

    private async Task ValidateStockAvailability(UpdateDeliveryRequest request, Guid? existingDeliveryId = null)
    {
        foreach (var detail in request.DeliveryDetails)
        {
            var bookingDetail = await _bookingDetailRepository.Query()
                .Include(bd => bd.BookingUnit)
                .FirstOrDefaultAsync(bd => bd.Id == detail.BookingDetailId);

            if (bookingDetail == null)
                throw new Exception($"Booking detail not found");

            // Calculate already delivered base quantity for this booking detail (excluding current delivery)
            var deliveredBaseQty = await ActiveDeliveryDetails()
                .Where(dd => dd.BookingDetailId == detail.BookingDetailId && dd.DeliveryId != existingDeliveryId)
                .SumAsync(dd => dd.BaseQuantity);

            // Calculate booking base quantity
            var bookingBaseQty = bookingDetail.BaseQuantity;

            // Calculate remaining base quantity
            var remainingBaseQty = bookingBaseQty - deliveredBaseQty;

            // Get delivery unit conversion to calculate delivery base quantity
            var deliveryUnit = await _unitConversionRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == detail.DeliveryUnitId);

            var deliveryBaseQty = deliveryUnit != null
                ? (decimal)(detail.DeliveryQuantity * deliveryUnit.ConversionValue)
                : (decimal)detail.DeliveryQuantity;

            if (deliveryBaseQty > remainingBaseQty)
            {
                // Calculate available in delivery unit for user-friendly error message
                var availableInDeliveryUnit = deliveryUnit != null && deliveryUnit.ConversionValue > 0
                    ? remainingBaseQty / (decimal)deliveryUnit.ConversionValue
                    : remainingBaseQty;

                throw new Exception($"Insufficient stock for product. Available: {availableInDeliveryUnit:F2} (in selected unit), Requested: {detail.DeliveryQuantity}");
            }
        }
    }

    private string GenerateNextNumber(string? lastNumber, string prefix)
    {
        if (string.IsNullOrEmpty(lastNumber))
            return $"{prefix}-{DateTime.UtcNow.Year}-0001";

        var parts = lastNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int number))
        {
            return $"{prefix}-{DateTime.UtcNow.Year}-{(number + 1):D4}";
        }

        return $"{prefix}-{DateTime.Now.Year}-0001";
    }

    private IQueryable<Delivery> ApplyOrdering(IQueryable<Delivery> queryable, string? orderBy, bool? isAscending)
    {
        Expression<Func<Delivery, object>> keySelector = orderBy?.ToLower() switch
        {
            "deliverynumber" => x => x.DeliveryNumber,
            "deliverydate" => x => x.DeliveryDate,
            _ => x => x.Id
        };

        return isAscending == true ? queryable.OrderBy(keySelector) : queryable.OrderByDescending(keySelector);
    }

    public async Task<BookingForDeliveryResponse> GetBookingForDeliveryAsync(string bookingNumber)
    {
        Guid guid = Guid.TryParse(bookingNumber, out var g) ? g : Guid.Empty;

        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .Include(b => b.Branch)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.BookingUnit)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.DeliveryDetails.Where(dd => !dd.IsDeleted && !dd.Delivery!.IsDeleted))
            .FirstOrDefaultAsync(b => b.Id == guid);

        if (booking == null)
            throw new Exception($"Booking with number '{bookingNumber}' not found");

        // // Get all deliveries for this booking to calculate remaining quantities
        // var deliveries = await _repository.Query()
        //     .Where(d => d.BookingId == booking.Id)
        //     .Include(d => d.DeliveryDetails)
        //     .ToListAsync();

        var response = new BookingForDeliveryResponse
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            ReferenceNumber = booking.ReferenceNumber,
            BookingDate = booking.BookingDate,
            CustomerId = booking.CustomerId,
            CustomerName = booking.Customer?.CustomerName,
            BranchId = booking.BranchId,
            BranchName = booking.Branch?.Name,
            Notes = booking.Notes,
            LastDeliveryDate = booking.BookingDetails.FirstOrDefault()?.LastDeliveryDate ?? default,
            BookingDetails = []
        };

        // Get available unit conversions - get the booking unit and all units with same base unit
        var bookingUnit = await _unitConversionRepository.Query().ToListAsync();

        foreach (var detail in booking.BookingDetails)
        {
            // Calculate total delivered base quantity for this booking detail
            var totalDeliveredBaseQty = detail.DeliveryDetails.Sum(dd => dd.BaseQuantity);

            // Calculate remaining in base units
            var remainingBaseQty = detail.BaseQuantity - totalDeliveredBaseQty;

            // Convert remaining base quantity back to booking unit
            var bookingUnitConversion = detail.BookingUnit?.ConversionValue ?? 1;
            var remainingQty = bookingUnitConversion > 0
                ? (float)(remainingBaseQty / (decimal)bookingUnitConversion)
                : 0;

            // Calculate total delivered in booking unit for display
            var totalDelivered = bookingUnitConversion > 0
                ? (float)(totalDeliveredBaseQty / (decimal)bookingUnitConversion)
                : 0;

            var unitConversions = new List<DeliveryUnitConversionResponse>();
            if (bookingUnit != null)
            {
                // Get all units that share the same base unit
                unitConversions = bookingUnit
                    .Where(uc => uc.BaseUnitId == detail.BookingUnit?.BaseUnitId || uc.Id == detail.BookingUnitId)
                    .Select(uc => new DeliveryUnitConversionResponse
                    {
                        Id = uc.Id,
                        UnitId = uc.BaseUnitId,
                        UnitName = uc.UnitName,
                        ConversionRate = (decimal)uc.ConversionValue,
                        IsBaseUnit = uc.BaseUnitId == uc.Id
                    })
                    .ToList();
            }

            // // Calculate charge per unit based on BillType
            // decimal totalCharge = detail.BillType.ToUpper() switch
            // {
            //     "MONTHLY" => detail.BookingRate / 30m, // Monthly rate divided by 30 days
            //     "DAILY" => detail.BookingRate,         // Daily rate as is
            //     "WEEKLY" => detail.BookingRate / 7m,   // Weekly rate divided by 7 days
            //     "YEARLY" => detail.BookingRate / 365m, // Yearly rate divided by 365 days
            //     "HOURLY" => detail.BookingRate * 24m,  // Hourly rate times 24 hours
            //     _ => detail.BookingRate / 30m           // Default to monthly
            // };

            response.BookingDetails.Add(new BookingDetailForDeliveryResponse
            {
                Id = detail.Id,
                ProductId = detail.ProductId,
                ProductName = detail.Product?.ProductName,
                BookingUnitId = detail.BookingUnitId,
                BookingUnitName = detail.BookingUnit?.UnitName,
                BookingQuantity = detail.BookingQuantity,
                BillType = detail.BillType,
                BookingRate = detail.BookingRate,
                BaseQuantity = detail.BaseQuantity,
                BaseRate = detail.BaseRate,
                TotalCharge = detail.BookingRate * (decimal)detail.BookingQuantity,
                LabourCharge = detail.LabourCharge,
                TotalDeliveredQuantity = totalDelivered,
                RemainingQuantity = remainingQty,
                AvailableUnits = unitConversions,
                LastDeliveryDate = detail.LastDeliveryDate
            });
        }

        return response;
    }

    public async Task<List<RemainingQuantityResponse>> GetRemainingQuantitiesAsync(Guid bookingId)
    {
        var bookingDetails = await _bookingDetailRepository.Query()
            .Include(bd => bd.BookingUnit)
            .Where(bd => bd.Booking != null && bd.Booking.Id == bookingId)
            .ToListAsync();

        var deliveries = await _repository.Query()
            .Where(d => d.BookingId == bookingId)
            .Include(d => d.DeliveryDetails)
            .ToListAsync();

        var results = new List<RemainingQuantityResponse>();

        foreach (var detail in bookingDetails)
        {
            // Calculate total delivered base quantity
            var totalDeliveredBaseQty = deliveries
                .SelectMany(d => d.DeliveryDetails)
                .Where(dd => dd.BookingDetailId == detail.Id)
                .Sum(dd => dd.BaseQuantity);

            // Calculate remaining in base units
            var remainingBaseQty = detail.BaseQuantity - totalDeliveredBaseQty;

            // Convert back to booking unit
            var bookingUnitConversion = detail.BookingUnit?.ConversionValue ?? 1;
            var totalDelivered = bookingUnitConversion > 0
                ? (float)(totalDeliveredBaseQty / (decimal)bookingUnitConversion)
                : 0;

            var remainingQty = bookingUnitConversion > 0
                ? (float)(remainingBaseQty / (decimal)bookingUnitConversion)
                : 0;

            results.Add(new RemainingQuantityResponse
            {
                BookingDetailId = detail.Id,
                BookingQuantity = detail.BookingQuantity,
                TotalDeliveredQuantity = totalDelivered,
                RemainingQuantity = remainingQty
            });
        }

        return results;
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetBookingLookupAsync()
    {
        return await _bookingRepository.Query()
            .Where(b => b.TenantId == _tenantId)
            .OrderByDescending(b => b.CreatedTime)
            .Select(b => new Lookup<Guid>(b.Id, b.BookingNumber + " - " + b.Customer.CustomerName))
            .ToListAsync();
    }

    public async Task<decimal> GetBookingPreviousPaymentsAsync(Guid bookingId)
    {
        // Get all deliveries for this booking
        var deliveries = await _repository.Query()
            .Where(d => d.BookingId == bookingId && !d.IsDeleted)
            .Select(d => new { d.Id, d.TransactionId })
            .ToListAsync();

        if (!deliveries.Any())
            return 0;

        var deliveryIds = deliveries.Select(d => d.Id).ToList();
        var deliveryTxIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();

        // Sum up all customer payment transactions for these deliveries
        var totalPaid = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT
                     && ((t.DeliveryId.HasValue && deliveryIds.Contains(t.DeliveryId.Value))
                         || deliveryTxIds.Contains(t.Id)
                         || (t.BookingId.HasValue && t.BookingId.Value == bookingId)))
            .SumAsync(t => t.Amount);

        return totalPaid;
    }

    public async Task<decimal> GetBookingDueAmountAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.Query()
            .Include(b => b.BookingDetails)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) return 0;

        var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();

        var deliveries = await _repository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => d.BookingId == bookingId && !d.IsDeleted)
            .ToListAsync();

        var (_, _, totalAccrued, _) = BookingDueCalculator.CalculateBookingAccruedDetails(
            booking,
            activeDetails,
            deliveries,
            DateTime.UtcNow);

        var deliveryIds = deliveries.Select(d => d.Id).ToList();
        var deliveryTxIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();

        var totalPaid = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                        && !t.IsArchived
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT
                        && ((t.DeliveryId.HasValue && deliveryIds.Contains(t.DeliveryId.Value))
                            || deliveryTxIds.Contains(t.Id)
                            || (t.BookingId.HasValue && t.BookingId.Value == bookingId)))
            .SumAsync(t => t.Amount);

        var totalDiscount = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                        && !t.IsArchived
                        && t.TransactionHead != null
                        && t.TransactionHead.UsageFor == UsageFor.BILL_DISCOUNT
                        && t.BookingId.HasValue && t.BookingId.Value == bookingId)
            .SumAsync(t => t.Amount > 0 ? t.Amount : t.DiscountAmount);

        return Math.Max(totalAccrued - totalPaid - totalDiscount, 0m);
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetDeliveryLookupAsync()
    {
        return await _repository.Query()
            .Where(d => d.TenantId == _tenantId)
            .OrderByDescending(d => d.CreatedTime)
            .Select(d => new Lookup<Guid>(d.Id, d.DeliveryNumber))
            .ToListAsync();
    }

    public async Task<DeliveryInvoiceResponse> GetInvoiceByIdAsync(Guid id)
    {
        var entity = await _repository.Query()
            .Include(x => x.Booking)
                .ThenInclude(b => b!.Customer)
            .Include(x => x.Booking)
                .ThenInclude(b => b!.BookingDetails)
                    .ThenInclude(bd => bd.Product)
            .Include(x => x.Branch)
            .Include(x => x.DeliveryDetails)
                .ThenInclude(d => d.BookingDetail)
                    .ThenInclude(bd => bd!.Product)
            .Include(x => x.DeliveryDetails)
                .ThenInclude(d => d.DeliveryUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception("Product delivery not found");

        var response = new DeliveryInvoiceResponse
        {
            Id = entity.Id,
            DeliveryNumber = entity.DeliveryNumber,
            DeliveryDate = entity.DeliveryDate,
            BookingId = entity.BookingId,
            BookingNumber = entity.Booking?.BookingNumber ?? "",
            BranchId = entity.BranchId,
            BranchName = entity.Branch?.Name,
            Notes = entity.Notes,
            ChargeAmount = entity.ChargeAmount,
            AdjustmentValue = entity.AdjustmentValue
        };

        // Map Customer Information
        if (entity.Booking?.Customer != null)
        {
            response.Customer = new CustomerBasicInfo
            {
                CustomerId = entity.Booking.Customer.Id,
                CustomerName = entity.Booking.Customer.CustomerName,
                CustomerMobile = entity.Booking.Customer.CustomerMobile,
                Address = entity.Booking.Customer.Address
            };
        }

        // Map Booking Information
        if (entity.Booking != null)
        {
            // Calculate total booking amount
            var totalBookingAmount = entity.Booking.BookingDetails?.Sum(bd => bd.BookingRate * (decimal)bd.BookingQuantity) ?? 0;

            // Calculate last delivery date from booking details
            var lastDeliveryDate = entity.Booking.BookingDetails?.FirstOrDefault()?.LastDeliveryDate ?? entity.Booking.BookingDate.AddDays(30);

            response.Booking = new BookingInvoiceInfo
            {
                BookingId = entity.BookingId,
                BookingNumber = entity.Booking.BookingNumber,
                ReferenceNumber = entity.Booking.ReferenceNumber,
                BookingDate = entity.Booking.BookingDate,
                LastDeliveryDate = lastDeliveryDate,
                TotalBookingAmount = totalBookingAmount
            };

            response.TotalBookingAmount = totalBookingAmount;
        }

        // Map Delivery Details with booking rate
        response.DeliveryDetails = entity.DeliveryDetails.Select(d => new DeliveryInvoiceDetailResponse
        {
            Id = d.Id,
            ProductId = d.BookingDetail?.ProductId ?? 0,
            ProductName = d.BookingDetail?.Product?.ProductName ?? "",
            DeliveryUnitId = d.DeliveryUnitId,
            DeliveryUnitName = d.DeliveryUnit?.UnitName ?? "",
            DeliveryQuantity = d.DeliveryQuantity,
            BaseQuantity = d.BaseQuantity,
            ChargeAmount = d.ChargeAmount,
            LabourCharge = d.LabourCharge,
            BookingRate = d.BookingDetail?.BookingRate ?? 0,
            BillingCycles = d.BillingCycles,
            BillType = d.BookingDetail?.BillType ?? ""
        }).ToList();

        // Financial summary based on immutable delivery-time CollectedAmount
        var totalDeliveryBill = response.DeliveryDetails.Sum(dd => dd.ChargeAmount + dd.LabourCharge) + entity.AdjustmentValue;
        response.TotalPaidAmount = entity.CollectedAmount;
        response.ExtraCharge = 0;
        response.DueAmount = Math.Max(0, totalDeliveryBill - entity.CollectedAmount);

        return response;
    }

    public async Task<List<DeliveryResponse>> GetUnpaidDeliveriesByCustomerAsync(int customerId)
    {
        var deliveries = await _repository.Query()
            .Where(x => x.Booking!.CustomerId == customerId && x.PaymentStatus == PaymentStatuses.UNPAID)
            .OrderBy(x => x.DeliveryDate)
            .Include(x => x.Booking)
                .ThenInclude(b => b!.Customer)
            .Include(x => x.DeliveryDetails)
            .ToListAsync();

        if (!deliveries.Any()) return [];

        var deliveryIds = deliveries.Select(d => d.Id).ToList();
        var deliveryTxIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();

        var payments = await _transactionRepository.Query()
            .Where(t => !t.IsDeleted
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT
                     && ((t.DeliveryId.HasValue && deliveryIds.Contains(t.DeliveryId.Value)) || deliveryTxIds.Contains(t.Id)))
            .Select(t => new { t.Id, t.DeliveryId, t.Amount })
            .ToListAsync();

        var result = new List<DeliveryResponse>();
        foreach (var x in deliveries)
        {
            var chargeAmount = x.DeliveryDetails.Sum(dd => dd.ChargeAmount);
            var labourCharge = x.DeliveryDetails.Sum(dd => dd.LabourCharge);
            var totalBill = chargeAmount + labourCharge + x.AdjustmentValue;
            var paidAmount = payments
                .Where(p => (p.DeliveryId.HasValue && p.DeliveryId.Value == x.Id) || (x.TransactionId.HasValue && p.Id == x.TransactionId.Value))
                .Sum(p => p.Amount);
            var dueAmount = Math.Max(0m, totalBill - paidAmount);

            if (dueAmount > 0.01m)
            {
                result.Add(new DeliveryResponse
                {
                    Id = x.Id,
                    DeliveryNumber = x.DeliveryNumber,
                    DeliveryDate = x.DeliveryDate,
                    BookingId = x.BookingId,
                    BookingNumber = x.Booking?.BookingNumber,
                    CustomerId = x.Booking?.CustomerId ?? customerId,
                    CustomerName = x.Booking?.Customer?.CustomerName,
                    BranchId = x.BranchId,
                    ChargeAmount = chargeAmount,
                    LabourCharge = labourCharge,
                    AdjustmentValue = x.AdjustmentValue,
                    PaidAmount = paidAmount,
                    DueAmount = dueAmount,
                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    TransactionId = x.TransactionId
                });
            }
        }

        return result;
    }

    public async Task<DeliveryResponse?> GetUnpaidDeliveryByCodeAsync(string deliveryCode)
    {
        var x = await _repository.Query()
            .Where(d => d.DeliveryNumber == deliveryCode && d.PaymentStatus == PaymentStatuses.UNPAID)
            .Include(d => d.Booking)
                .ThenInclude(b => b!.Customer)
            .Include(d => d.DeliveryDetails)
            .FirstOrDefaultAsync();

        if (x == null) return null;

        var chargeAmount = x.DeliveryDetails.Sum(dd => dd.ChargeAmount);
        var labourCharge = x.DeliveryDetails.Sum(dd => dd.LabourCharge);
        var totalBill = chargeAmount + labourCharge + x.AdjustmentValue;

        var paidAmount = await _transactionRepository.Query()
            .Where(t => !t.IsDeleted
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT
                     && ((t.DeliveryId.HasValue && t.DeliveryId.Value == x.Id) || (x.TransactionId.HasValue && t.Id == x.TransactionId.Value)))
            .SumAsync(t => t.Amount);

        var dueAmount = Math.Max(0m, totalBill - paidAmount);

        return new DeliveryResponse
        {
            Id = x.Id,
            DeliveryNumber = x.DeliveryNumber,
            DeliveryDate = x.DeliveryDate,
            BookingId = x.BookingId,
            BookingNumber = x.Booking?.BookingNumber,
            CustomerId = x.Booking?.CustomerId ?? 0,
            CustomerName = x.Booking?.Customer?.CustomerName,
            BranchId = x.BranchId,
            ChargeAmount = chargeAmount,
            LabourCharge = labourCharge,
            AdjustmentValue = x.AdjustmentValue,
            PaidAmount = paidAmount,
            DueAmount = dueAmount,
            PaymentStatus = x.PaymentStatus,
            PaymentDate = x.PaymentDate,
            TransactionId = x.TransactionId
        };
    }

    public async Task<List<DeliveryResponse>> GetAllUnpaidDeliveriesAsync()
    {
        var deliveries = await _repository.Query()
            .Where(x => x.PaymentStatus == PaymentStatuses.UNPAID)
            .OrderBy(x => x.DeliveryDate)
            .Include(x => x.Booking)
                .ThenInclude(b => b!.Customer)
            .Include(x => x.DeliveryDetails)
            .ToListAsync();

        if (!deliveries.Any()) return [];

        var deliveryIds = deliveries.Select(d => d.Id).ToList();
        var deliveryTxIds = deliveries.Where(d => d.TransactionId.HasValue).Select(d => d.TransactionId!.Value).ToList();

        var payments = await _transactionRepository.Query()
            .Where(t => !t.IsDeleted
                     && t.TransactionHead != null
                     && t.TransactionHead.UsageFor == UsageFor.CUSTOMER_PAYMENT
                     && ((t.DeliveryId.HasValue && deliveryIds.Contains(t.DeliveryId.Value)) || deliveryTxIds.Contains(t.Id)))
            .Select(t => new { t.Id, t.DeliveryId, t.Amount })
            .ToListAsync();

        var result = new List<DeliveryResponse>();
        foreach (var x in deliveries)
        {
            var chargeAmount = x.DeliveryDetails.Sum(dd => dd.ChargeAmount);
            var labourCharge = x.DeliveryDetails.Sum(dd => dd.LabourCharge);
            var totalBill = chargeAmount + labourCharge + x.AdjustmentValue;
            var paidAmount = payments
                .Where(p => (p.DeliveryId.HasValue && p.DeliveryId.Value == x.Id) || (x.TransactionId.HasValue && p.Id == x.TransactionId.Value))
                .Sum(p => p.Amount);
            var dueAmount = Math.Max(0m, totalBill - paidAmount);

            if (dueAmount > 0.01m)
            {
                result.Add(new DeliveryResponse
                {
                    Id = x.Id,
                    DeliveryNumber = x.DeliveryNumber,
                    DeliveryDate = x.DeliveryDate,
                    BookingId = x.BookingId,
                    BookingNumber = x.Booking?.BookingNumber,
                    CustomerId = x.Booking?.CustomerId ?? 0,
                    CustomerName = x.Booking?.Customer?.CustomerName,
                    BranchId = x.BranchId,
                    ChargeAmount = chargeAmount,
                    LabourCharge = labourCharge,
                    AdjustmentValue = x.AdjustmentValue,
                    PaidAmount = paidAmount,
                    DueAmount = dueAmount,
                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    TransactionId = x.TransactionId
                });
            }
        }

        return result;
    }

    public async Task<List<DeliveryResponse>> GetAllDeliveriesAsync()
    {
        var deliveries = await _repository.Query()
            .OrderByDescending(x => x.DeliveryDate)
            .Select(x => new DeliveryResponse
            {
                Id = x.Id,
                DeliveryNumber = x.DeliveryNumber,
                DeliveryDate = x.DeliveryDate,
                BookingId = x.BookingId,
                BookingNumber = x.Booking.BookingNumber,
                CustomerId = x.Booking.CustomerId,
                CustomerName = x.Booking.Customer.CustomerName,
                BranchId = x.BranchId,
                ChargeAmount = x.DeliveryDetails.Sum(dd => dd.ChargeAmount),
                LabourCharge = x.DeliveryDetails.Sum(dd => dd.LabourCharge),
                AdjustmentValue = x.AdjustmentValue,
                PaymentStatus = x.PaymentStatus,
                PaymentDate = x.PaymentDate,
                TransactionId = x.TransactionId
            })
            .ToListAsync();

        return deliveries;
    }

    public async Task<List<DeliveryResponse>> GetDeliveriesByTransactionIdAsync(Guid transactionId)
    {
        var deliveries = await _repository.Query()
            .Where(x => x.TransactionId == transactionId)
            .OrderBy(x => x.DeliveryDate)
            .Select(x => new DeliveryResponse
            {
                Id = x.Id,
                DeliveryNumber = x.DeliveryNumber,
                DeliveryDate = x.DeliveryDate,
                BookingId = x.BookingId,
                BookingNumber = x.Booking.BookingNumber,
                CustomerId = x.Booking.CustomerId,
                CustomerName = x.Booking.Customer.CustomerName,
                BranchId = x.BranchId,
                ChargeAmount = x.DeliveryDetails.Sum(dd => dd.ChargeAmount),
                LabourCharge = x.DeliveryDetails.Sum(dd => dd.LabourCharge),
                AdjustmentValue = x.AdjustmentValue,
                PaymentStatus = x.PaymentStatus,
                PaymentDate = x.PaymentDate,
                TransactionId = x.TransactionId
            })
            .ToListAsync();

        return deliveries;
    }
}
