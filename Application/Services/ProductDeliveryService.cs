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
        // entity.DeliveryDate = DateTime.UtcNow;
        _defaultValueInjector.InjectCreatingAudit<Delivery, Guid>(entity);


        if (entity.DeliveryDetails != null && entity.DeliveryDetails.Any())
        {

            // var bookingDetails = await _bookingDetailRepository.Query()
            //     .Where(bd => entity.DeliveryDetails.Select(dd => dd.BookingDetailId).Contains(bd.Id))
            //     .ToListAsync(cancellationToken);

            foreach (var detail in entity.DeliveryDetails)
            {
                // Calculate BaseQuantity and BaseRate from unit conversion
                var unitConversion = await _unitConversionRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == detail.DeliveryUnitId, cancellationToken);

                if (unitConversion != null)
                {
                    detail.BaseQuantity = (decimal)(detail.DeliveryQuantity * unitConversion.ConversionValue);
                }
                else
                {
                    detail.BaseQuantity = (decimal)detail.DeliveryQuantity;
                }


                _defaultValueInjector.InjectCreatingAudit<DeliveryDetail, Guid>(entity.DeliveryDetails.ToList());
            }
        }

        // Calculate total labour and rent charges from delivery details
        var totalLabourCharge = entity.DeliveryDetails?.Sum(d => d.LabourCharge) ?? 0m;
        var totalRentCharge = entity.DeliveryDetails?.Sum(d => d.ChargeAmount + d.AdjustmentValue) ?? 0m;
        var deliveryGrandTotal = totalRentCharge + totalLabourCharge;

        var paymentAmount = request.TransactionAmount.HasValue && request.TransactionAmount.Value > 0
            ? request.TransactionAmount.Value
            : 0m;

        entity.CollectedAmount = paymentAmount;
        if (entity.DeliveryDetails != null && entity.DeliveryDetails.Any())
        {
            if (entity.DeliveryDetails.Count == 1)
            {
                entity.DeliveryDetails.First().CollectedAmount = paymentAmount;
            }
            else
            {
                var remainingToAllocate = paymentAmount;
                var detailsList = entity.DeliveryDetails.ToList();
                for (int i = 0; i < detailsList.Count; i++)
                {
                    if (i == detailsList.Count - 1)
                    {
                        detailsList[i].CollectedAmount = remainingToAllocate;
                    }
                    else
                    {
                        var detailTotal = detailsList[i].ChargeAmount + detailsList[i].LabourCharge + detailsList[i].AdjustmentValue;
                        var share = deliveryGrandTotal > 0
                            ? Math.Round(paymentAmount * (detailTotal / deliveryGrandTotal), 2)
                            : 0m;
                        share = Math.Min(share, remainingToAllocate);
                        detailsList[i].CollectedAmount = share;
                        remainingToAllocate -= share;
                    }
                }
            }
        }

        // Set initial payment status
        entity.PaymentStatus = PaymentStatuses.UNPAID;
        entity.PaymentDate = null;
        entity.TransactionId = null;

        await _repository.AddAsync(entity, CancellationToken.None);

        // Create transactions if collection amount is provided and > 0
        if (request.TransactionAmount.HasValue && request.TransactionAmount.Value > 0)
        {
            if (paymentAmount > deliveryGrandTotal + 0.01m)
            {
                throw new Exception($"Collection amount (৳{paymentAmount:N2}) cannot exceed delivery total (৳{deliveryGrandTotal:N2})");
            }

            var booking = await _bookingRepository.Query()
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId);

            // Allocate between labour and rent: labour first
            var labourPayment = Math.Min(totalLabourCharge, paymentAmount);
            var rentPayment = paymentAmount - labourPayment;

            // Generate sequential transaction code
            var currentDate = DateTime.UtcNow;
            var datePart = currentDate.ToString("yyMMdd");
            var prefix = "DEL";

            var existingCodes = await _transactionRepository.Query()
                .Where(x => x.TransactionCode.StartsWith($"{prefix}-{datePart}-"))
                .Select(x => x.TransactionCode)
                .ToListAsync(cancellationToken);

            int maxSequence = 0;
            var seqPattern = new System.Text.RegularExpressions.Regex($@"^{System.Text.RegularExpressions.Regex.Escape(prefix)}-{datePart}-(\d+)");
            foreach (var code in existingCodes)
            {
                var match = seqPattern.Match(code);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int seq))
                {
                    if (seq > maxSequence) maxSequence = seq;
                }
            }

            int nextSequence = maxSequence + 1;
            var baseTransactionCode = CodeGenerator.GenerateTransactionCode(prefix, nextSequence);
            Guid? mainTxId = null;

            // Single unified CUSTOMER_PAYMENT transaction (if any payment made at delivery)
            decimal totalDeliveryPayment = rentPayment + labourPayment;
            if (totalDeliveryPayment > 0)
            {
                var transactionHead = await _transactionHeadRepository.Query()
                    .FirstOrDefaultAsync(th => th.Type == TransactionHeadTypes.DEBIT && th.UsageFor == UsageFor.CUSTOMER_PAYMENT && th.IsActive);

                if (transactionHead == null)
                    throw new Exception("CUSTOMER_PAYMENT transaction head not found");

                var chargeTransactionRequest = new TransactionRequest(
                    Id: Guid.NewGuid(),
                    TransactionCode: baseTransactionCode,
                    TransactionDate: DateTime.UtcNow,
                    TransactionHeadId: transactionHead.Id,
                    BranchId: entity.BranchId,
                    CustomerId: booking?.CustomerId,
                    BookingId: request.BookingId,
                    DeliveryId: entity.Id,
                    Amount: totalDeliveryPayment,
                    DiscountAmount: 0,
                    AdjustmentValue: 0,
                    NetAmount: totalDeliveryPayment,
                    PaymentMethod: request.PaymentMethod ?? PaymentMethods.CASH,
                    PaymentReference: request.PaymentReference,
                    BankId: request.BankId,
                    Category: null,
                    SubCategory: null,
                    Description: $"Payment for Delivery {entity.DeliveryNumber}",
                    Note: request.TransactionNotes
                );

                var chargeTransaction = await _transactionService.AddAsync(chargeTransactionRequest, CancellationToken.None);
                mainTxId = chargeTransaction.Id;
            }

            // If paid by bank or cheque, automatically record deposit in BankTransaction
            if (request.PaymentMethod != PaymentMethods.CASH && request.BankId.HasValue)
            {
                var bank = await _bankRepository.GetByIdAsync(request.BankId.Value, cancellationToken);
                if (bank != null)
                {
                    var currentBalance = bank.OpeningBalance + await _bankTransactionRepository.Query()
                        .Where(bt => bt.BankId == request.BankId.Value && bt.IsActive && !bt.IsDeleted)
                        .SumAsync(bt => bt.TransactionType == BankTransactionTypes.Deposit ? bt.Amount : -bt.Amount, cancellationToken);

                    var bankTx = new BankTransaction
                    {
                        TransactionNumber = baseTransactionCode,
                        TransactionDate = DateTime.UtcNow,
                        BankId = request.BankId.Value,
                        TransactionType = BankTransactionTypes.Deposit,
                        Amount = paymentAmount,
                        Reference = request.PaymentReference,
                        Description = $"Delivery Payment - {entity.DeliveryNumber} ({request.PaymentMethod})",
                        BalanceAfter = currentBalance + paymentAmount,
                        SourceType = BankSourceTypes.DELIVERY,
                        TransactionId = mainTxId,
                        BranchId = entity.BranchId,
                        IsActive = true
                    };
                    _defaultValueInjector.InjectCreatingAudit<BankTransaction, long>(bankTx);
                    await _bankTransactionRepository.AddAsync(bankTx, cancellationToken);
                }
            }

            entity.TransactionId = mainTxId;
            entity.CollectedAmount = paymentAmount;
            if (paymentAmount >= deliveryGrandTotal - 0.01m)
            {
                entity.PaymentStatus = PaymentStatuses.PAID;
                entity.PaymentDate = DateTime.UtcNow;
            }
            else
            {
                entity.PaymentStatus = PaymentStatuses.UNPAID;
                entity.PaymentDate = null;
            }

            if (entity.DeliveryDetails.Any())
            {
                decimal remainingToDistribute = paymentAmount;
                foreach (var detail in entity.DeliveryDetails)
                {
                    var detailDue = detail.ChargeAmount + detail.LabourCharge;
                    var allocated = Math.Min(remainingToDistribute, detailDue);
                    detail.CollectedAmount = allocated;
                    remainingToDistribute -= allocated;
                }
            }

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
        // Use tracked query for update operations
        var existing = await _repository.UpdatableQuery(x => x.Id == id)
            .Include(x => x.DeliveryDetails)
            .FirstOrDefaultAsync();

        if (existing == null)
            throw new Exception("Product delivery not found");

        // Validate stock availability (considering current delivery)
        await ValidateStockAvailability(request, id);

        // Update main entity properties manually (avoid Adapt to prevent collection issues)
        existing.DeliveryNumber = request.DeliveryNumber;
        existing.DeliveryDate = request.DeliveryDate;
        existing.Notes = request.Notes;
        existing.ChargeAmount = request.ChargeAmount;
        existing.AdjustmentValue = request.AdjustmentValue;
        _defaultValueInjector.InjectUpdatingAudit<Delivery, Guid>(existing);

        // Update child collection: Clear and add new ones (cascade delete handles removal)
        existing.DeliveryDetails.Clear();



        // Add new details
        if (request.DeliveryDetails != null && request.DeliveryDetails.Any())
        {
            foreach (var d in request.DeliveryDetails)
            {
                var newDetail = new DeliveryDetail
                {
                    DeliveryId = existing.Id,
                    BookingDetailId = d.BookingDetailId,
                    DeliveryUnitId = d.DeliveryUnitId,
                    DeliveryQuantity = d.DeliveryQuantity,
                    BaseQuantity = d.BaseQuantity,
                    ChargeAmount = d.ChargeAmount,
                    LabourCharge = d.LabourCharge,
                    AdjustmentValue = d.AdjustmentValue,
                    BillingCycles = d.BillingCycles
                };
                _defaultValueInjector.InjectCreatingAudit<DeliveryDetail, Guid>(new List<DeliveryDetail> { newDetail });
                existing.DeliveryDetails.Add(newDetail);
            }
        }

        await _repository.UpdateAsync(existing, CancellationToken.None);

        // Query fresh data after update since Query() uses AsNoTracking()
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await _repository.DeletableQuery(x => x.Id == id).ExecuteDeleteAsync();
        var result1 = await _transactionRepository.DeletableQuery(x => x.DeliveryId == id)
            .ExecuteDeleteAsync();
        return result > 0;
    }

    public async Task<bool> BatchDeleteAsync(Guid[] ids)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        return result > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Product delivery not found");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);

        // Soft-delete linked transactions to prevent ghost revenue
        if (entity.TransactionId.HasValue)
        {
            var linkedTransactions = await _transactionRepository.UpdatableQuery(
                t => t.DeliveryId == id && !t.IsDeleted).ToListAsync(cancellationToken);
            foreach (var txn in linkedTransactions)
            {
                txn.IsDeleted = true;
                txn.DeletedAt = DateTime.UtcNow;
                txn.DeletedById = _currentUser.Id;
                await _transactionRepository.UpdateAsync(txn, cancellationToken);
            }
        }

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Product delivery not found");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

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
            var deliveredBaseQty = await _detailRepository.Query()
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
            var deliveredBaseQty = await _detailRepository.Query()
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
                .ThenInclude(bd => bd.DeliveryDetails)
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
