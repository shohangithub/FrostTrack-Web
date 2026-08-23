namespace Application.Services;

public class BookingService : IBookingService
{
    private readonly IRepository<Booking, Guid> _repository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly IRepository<Customer, int> _customerRepository;
    private readonly IRepository<UnitConversion, int> _unitConversionRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly IRepository<RecurringChargeEntry, Guid> _recurringChargeEntryRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public BookingService(
        IRepository<Booking, Guid> repository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        IRepository<Company, int> companyRepository,
        IRepository<Customer, int> customerRepository,
        IRepository<UnitConversion, int> unitConversionRepository,
        IBookingRepository bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        IRepository<RecurringChargeEntry, Guid> recurringChargeEntryRepository,
        ICodeGenerationService codeGenerationService)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _customerRepository = customerRepository;
        _unitConversionRepository = unitConversionRepository;
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _transactionRepository = transactionRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _recurringChargeEntryRepository = recurringChargeEntryRepository;
        _codeGenerationService = codeGenerationService;
    }

    public async Task<BookingResponse> AddAsync(BookingRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BookingValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            BookingValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        var entity = request.Adapt<Booking>();
        entity.BranchId = _currentUser.BranchId;



        entity.BookingDate = request.BookingDate.Kind == DateTimeKind.Utc
    ? request.BookingDate
    : request.BookingDate.ToUniversalTime();


        _defaultValueInjector.InjectCreatingAudit<Booking, Guid>(entity);
        if (entity.BookingDetails != null && entity.BookingDetails.Any())
        {
            foreach (var detail in entity.BookingDetails)
            {
                detail.BillType = BillTypes.Monthly; // Set default BillType

                // Calculate BaseQuantity and BaseRate from unit conversion
                var unitConversion = await _unitConversionRepository.Query()
                    .FirstOrDefaultAsync(x => x.Id == detail.BookingUnitId, cancellationToken);

                if (unitConversion != null)
                {
                    detail.BaseQuantity = (decimal)(detail.BookingQuantity * unitConversion.ConversionValue);
                    detail.BaseRate = detail.BookingRate / (decimal)unitConversion.ConversionValue;
                }
                else
                {
                    detail.BaseQuantity = (decimal)detail.BookingQuantity;
                    detail.BaseRate = detail.BookingRate;
                }

                // Calculate LastDeliveryDate based on BillType
                detail.LastDeliveryDate = CalculateLastDeliveryDate(entity.BookingDate, detail.BillType);
            }
            _defaultValueInjector.InjectCreatingAudit<BookingDetail, Guid>(entity.BookingDetails.ToList());
        }

        await _repository.AddAsync(entity, cancellationToken);

        // Create INITIAL RecurringChargeEntry for each booking detail (first billing cycle)
        if (entity.BookingDetails != null && entity.BookingDetails.Any())
        {
            var now = DateTime.UtcNow;
            foreach (var detail in entity.BookingDetails.Where(d => !d.IsDeleted))
            {
                var periodEnd = CalculateLastDeliveryDate(entity.BookingDate, detail.BillType);
                var amount = (decimal)detail.BookingQuantity * detail.BookingRate;
                var entry = new RecurringChargeEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantId,
                    BookingId = entity.Id,
                    BookingDetailId = detail.Id,
                    RecurringChargeRunId = null,
                    Source = RecurringChargeSources.Initial,
                    BillPeriodFrom = entity.BookingDate,
                    BillPeriodTo = periodEnd,
                    BillType = detail.BillType,
                    Cycles = 1,
                    Quantity = detail.BookingQuantity,
                    Rate = detail.BookingRate,
                    Amount = amount,
                    Note = $"Initial charge on booking {entity.BookingNumber}",
                    CreatedAt = now,
                };
                await _recurringChargeEntryRepository.AddAsync(entry, cancellationToken);
            }

            // Create a Transaction record (accounts receivable) for the booking charge
            // This makes the customer's storage obligation visible in the accounting ledgers.
            var storageChargeHead = await _transactionHeadRepository.Query()
                .FirstOrDefaultAsync(th => th.Code == "STORAGE_CHARGE" && th.IsActive, cancellationToken);

            if (storageChargeHead != null)
            {
                var totalChargeAmount = entity.BookingDetails
                    .Where(d => !d.IsDeleted)
                    .Sum(d => (decimal)d.BookingQuantity * d.BookingRate + d.LabourCharge);

                if (totalChargeAmount > 0)
                {
                    var currentDate = DateTime.UtcNow;
                    var datePart = currentDate.ToString("yyMMdd");
                    var prefix = "BKC";

                    var lastCode = await _transactionRepository.Query()
                        .Where(x => x.TransactionCode.StartsWith($"{prefix}-{datePart}-"))
                        .OrderByDescending(x => x.TransactionCode)
                        .Select(x => x.TransactionCode)
                        .FirstOrDefaultAsync(cancellationToken);

                    int nextSequence = 1;
                    if (!string.IsNullOrEmpty(lastCode))
                    {
                        var parts = lastCode.Split('-');
                        if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                        {
                            nextSequence = lastSequence + 1;
                        }
                    }

                    var transactionCode = CodeGenerator.GenerateTransactionCode(prefix, nextSequence);
                    var chargeTransaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        TenantId = _tenantId,
                        TransactionCode = transactionCode,
                        TransactionDate = entity.BookingDate,
                        TransactionHeadId = storageChargeHead.Id,
                        BranchId = entity.BranchId,
                        CustomerId = entity.CustomerId,
                        BookingId = entity.Id,
                        Amount = totalChargeAmount,
                        DiscountAmount = 0,
                        AdjustmentValue = 0,
                        NetAmount = totalChargeAmount,
                        PaymentMethod = PaymentMethods.CREDIT, // On account — customer pays later
                        Description = $"Storage charge for Booking {entity.BookingNumber}",
                        Note = $"Auto-generated on booking creation",
                    };
                    _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(chargeTransaction);
                    await _transactionRepository.AddAsync(chargeTransaction, cancellationToken);
                }
            }
        }

        var response = entity.Adapt<BookingResponse>();
        return response;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Booking record not found!");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Booking record not found!");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Booking record not found!");

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Booking record not found!");

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<BookingResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.Product)
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.BookingUnit)
            .ThenInclude(x => x.BaseUnit)
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<BookingResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(predicate)
            .Select(x => new Lookup<Guid>(x.Id, x.BookingNumber))
            .ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<BookingResponse> UpdateAsync(Guid id, BookingRequest request, CancellationToken cancellationToken = default)
    {
        BookingValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _bookingRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Booking record not found!");

        existingData.BranchId = _currentUser.BranchId;
        existingData.CustomerId = request.CustomerId;
        existingData.Notes = request.Notes;
        existingData.BookingDate = request.BookingDate.Kind == DateTimeKind.Utc
        ? request.BookingDate : request.BookingDate.ToUniversalTime();

        _defaultValueInjector.InjectUpdatingAudit<Booking, Guid>(existingData);

        var response = await _bookingRepository.ManageUpdate(request, existingData, cancellationToken);

        return response;
    }

    public async Task<IEnumerable<BookingListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Include(x => x.Customer)
           .Include(x => x.Branch)
           .Select(x => new BookingListResponse(
                x.Id,
                x.BookingNumber,
                x.ReferenceNumber,
                x.BookingDate,
                x.CustomerId,
                x.Customer!,
                x.BranchId,
                x.Branch!,
                x.Notes,
                x.IsDeleted,
                x.IsArchived,
                x.DeletedAt,
                x.ArchivedAt,
                x.BookingDetails.Select(d => new BookingDetailListResponse(
                    d.Id,
                    d.Id,
                    d.ProductId,
                    d.Product!.ProductName,
                    d.BookingUnitId,
                    d.BookingUnit!.UnitName,
                    d.BookingQuantity,
                    d.BillType,
                    d.BookingRate,
                    d.BaseQuantity,
                    d.BaseRate,
                    d.LabourCharge,
                    d.LastDeliveryDate))
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<BookingListResponse>> PaginationListAsync(BookingPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        // Map frontend column names to entity property names
        if (!string.IsNullOrEmpty(requestQuery.OrderBy))
        {
            var mappedOrderBy = requestQuery.OrderBy switch
            {
                "bookingNumber" => nameof(Booking.BookingNumber),
                "bookingDate" => nameof(Booking.BookingDate),
                "notes" => nameof(Booking.Notes),
                _ => requestQuery.OrderBy
            };
            requestQuery = requestQuery with { OrderBy = mappedOrderBy };
        }

        var status = requestQuery.Status?.ToLowerInvariant() ?? "active";

        Expression<Func<Booking, bool>> predicate = x => true;

        predicate = status switch
        {
            "archived" => predicate.And(x => !x.IsDeleted && x.IsArchived),
            "deleted" => predicate.And(x => x.IsDeleted && x.TenantId == _tenantId),
            _ => predicate.And(x => !x.IsDeleted && !x.IsArchived)
        };

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchText = requestQuery.OpenText.ToLower();
            predicate = predicate.And(obj => obj.BookingNumber.ToLower().Contains(searchText)
                            || (obj.Customer != null && obj.Customer.CustomerName.ToLower().Contains(searchText)));
        }

        Expression<Func<Booking, BookingListResponse>>? selector = x => new BookingListResponse(
            x.Id,
            x.BookingNumber,
            x.ReferenceNumber,
            x.BookingDate,
            x.CustomerId,
            x.Customer!,
            x.BranchId,
            x.Branch!,
            x.Notes,
            x.IsDeleted,
            x.IsArchived,
            x.DeletedAt,
            x.ArchivedAt,
            x.BookingDetails.Select(d => new BookingDetailListResponse(
                d.Id,
                d.Id,
                d.ProductId,
                d.Product!.ProductName,
                d.BookingUnitId,
                d.BookingUnit!.UnitName,
                d.BookingQuantity,
                d.BillType,
                d.BookingRate,
                d.BaseQuantity,
                d.BaseRate,
                d.LabourCharge,
                d.LastDeliveryDate))
            );

        var baseQuery = status == "deleted"
            ? _repository.UnfilteredQuery()
            : _bookingRepository.Query();

        var query = baseQuery;

        query = query.Where(predicate);

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    private DateTime CalculateLastDeliveryDate(DateTime bookingDate, string billType)
    {
        return billType switch
        {
            BillTypes.Hourly => bookingDate.AddHours(1),
            BillTypes.Daily => bookingDate.AddDays(1),
            BillTypes.Weekly => bookingDate.AddDays(7),
            BillTypes.Monthly => bookingDate.AddMonths(1),
            BillTypes.Yearly => bookingDate.AddYears(1),
            _ => bookingDate.AddMonths(1) // Default to monthly
        };
    }

    public async Task<string> GenerateBookingNumber(CancellationToken cancellationToken = default)
    {
        return await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "BK",
            b => b.BookingNumber,
            cancellationToken);
    }

    public async Task<BookingInvoiceWithDeliveryResponse?> GetInvoiceWithDeliveryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .Include(x => x.BookingDetails)
                .ThenInclude(bd => bd.Product)
            .Include(x => x.BookingDetails)
                .ThenInclude(bd => bd.BookingUnit)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (booking == null)
            return null;

        var response = new BookingInvoiceWithDeliveryResponse
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            BookingDate = booking.BookingDate,
            CustomerId = booking.CustomerId,
            Customer = booking.Customer,
            BranchId = booking.BranchId,
            Branch = booking.Branch,
            Notes = booking.Notes,
            BookingDetails = booking.BookingDetails.Select(bd => new BookingDetailWithDeliveryResponse
            {
                Id = bd.Id,
                BookingId = booking.Id,
                ProductId = bd.ProductId,
                Product = bd.Product?.Adapt<ProductResponse>(),
                BookingUnitId = bd.BookingUnitId,
                BookingUnit = bd.BookingUnit?.Adapt<UnitConversionResponse>(),
                BookingQuantity = bd.BookingQuantity,
                BillType = bd.BillType,
                BookingRate = bd.BookingRate,
                BaseQuantity = bd.BaseQuantity,
                BaseRate = bd.BaseRate,
                LastDeliveryDate = bd.LastDeliveryDate,
                LabourCharge = bd.LabourCharge
            }).ToList()
        };

        // Get all transactions for this booking
        var transactions = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.BookingId == id && t.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION && t.TransactionHead!.Type == TransactionHeadTypes.DEBIT)
            .ToListAsync(cancellationToken);

        // Get all deliveries for this booking
        var deliveries = await _deliveryRepository.Query()
            .Where(d => d.BookingId == id && d.TenantId == _tenantId)
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.BookingDetail)
                    .ThenInclude(bd => bd!.Product)
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.DeliveryUnit)
            .OrderBy(d => d.DeliveryDate)
            .ToListAsync(cancellationToken);

        response.Deliveries = deliveries.Select(d => new DeliveryInfoResponse
        {
            Id = d.Id,
            DeliveryNumber = d.DeliveryNumber,
            DeliveryDate = d.DeliveryDate,
            ChargeAmount = transactions.FirstOrDefault(t => t.DeliveryId == d.Id)?.Amount ?? 0,
            AdjustmentValue = d.AdjustmentValue,
            DeliveryDetails = d.DeliveryDetails.Select(dd => new DeliveryDetailInfoResponse
            {
                Id = dd.Id,
                ProductId = dd.BookingDetail?.ProductId ?? 0,
                ProductName = dd.BookingDetail?.Product?.ProductName ?? "",
                DeliveryUnitId = dd.DeliveryUnitId,
                DeliveryUnitName = dd.DeliveryUnit?.UnitName ?? "",
                DeliveryQuantity = dd.DeliveryQuantity,
                BaseQuantity = dd.BaseQuantity,
                ChargeAmount = dd.ChargeAmount,
                LabourCharge = dd.LabourCharge
            }).ToList()
        }).ToList();

        return response;
    }

    public async Task<IEnumerable<CustomerDueSummaryResponse>> GetCustomerDueSummaryAsync(CancellationToken cancellationToken = default)
    {
        var bookings = await _repository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
            .Where(b => !b.IsDeleted && !b.IsArchived)
            .ToListAsync(cancellationToken);

        if (!bookings.Any()) return [];

        var bookingIds = bookings.Select(b => b.Id).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId) && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var payments = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                        && t.BookingId.HasValue
                        && bookingIds.Contains(t.BookingId.Value)
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && (t.TransactionHead.UsageFor == UsageFor.BILL_COLLECTION
                            || t.TransactionHead.UsageFor == UsageFor.LABOUR_CHARGE))
            .Select(t => new
            {
                BookingId = t.BookingId!.Value,
                t.Amount,
                t.TransactionDate
            })
            .ToListAsync(cancellationToken);

        var paymentsByBooking = payments
            .GroupBy(x => x.BookingId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var deliveryAccruedByBooking = deliveries
            .GroupBy(d => d.BookingId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(d => d.ChargeAmount + d.AdjustmentValue + (d.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0m))
            );

        var customerGroups = bookings.GroupBy(b => b.CustomerId);
        var summaries = new List<CustomerDueSummaryResponse>();
        var now = DateTime.UtcNow;

        foreach (var group in customerGroups)
        {
            var customerBookings = group.ToList();
            var customer = customerBookings.First().Customer;
            var openingBalance = customer?.OpeningBalance ?? 0m;

            decimal totalAccrued = openingBalance;
            decimal totalPaid = 0m;
            decimal totalPendingRecurringCharge = 0m;

            foreach (var booking in customerBookings)
            {
                var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();
                var deliveryCharge = deliveryAccruedByBooking.TryGetValue(booking.Id, out var val) ? val : 0m;
                
                var (bookingAccrued, pendingRecurringCharge) = Common.BookingDueCalculator.CalculateBookingAccruedAmount(
                    booking, 
                    activeDetails, 
                    deliveryCharge, 
                    now);

                totalAccrued += bookingAccrued;
                totalPendingRecurringCharge += pendingRecurringCharge;

                if (paymentsByBooking.TryGetValue(booking.Id, out var paid))
                {
                    totalPaid += paid;
                }
            }

            var totalDue = Math.Max(totalAccrued - totalPaid, 0m);

            var oldestBooking = customerBookings.OrderBy(b => b.BookingDate).First();
            var daysSinceOldestBooking = (now - oldestBooking.BookingDate).Days;

            // Determine last payment date across all bookings of this customer
            var customerBookingIds = customerBookings.Select(b => b.Id).ToHashSet();
            var lastPaymentDate = payments
                .Where(p => customerBookingIds.Contains(p.BookingId))
                .Select(p => (DateTime?)p.TransactionDate)
                .DefaultIfEmpty(null)
                .Max();
            var daysSinceLastPayment = lastPaymentDate.HasValue
                ? (now - lastPaymentDate.Value).Days
                : daysSinceOldestBooking;

            var status = "normal";
            if (totalDue > 0 && daysSinceLastPayment >= 30) status = "danger";
            else if (totalDue > 0 && daysSinceLastPayment >= 25) status = "warning";

            summaries.Add(new CustomerDueSummaryResponse
            {
                CustomerId = group.Key,
                CustomerName = customer?.CustomerName ?? string.Empty,
                CustomerMobile = customer?.CustomerMobile ?? string.Empty,
                CustomerAddress = customer?.Address ?? string.Empty,
                TotalBookings = customerBookings.Count,
                OpeningBalance = openingBalance,
                TotalAmount = totalAccrued,
                PendingRecurringChargeAmount = totalPendingRecurringCharge,
                TotalPaid = totalPaid,
                TotalDue = totalDue,
                OldestBookingDate = oldestBooking.BookingDate,
                DaysSinceOldestBooking = daysSinceOldestBooking,
                LastPaymentDate = lastPaymentDate,
                DaysSinceLastPayment = daysSinceLastPayment,
                Status = status
            });
        }

        return summaries.OrderByDescending(c => c.TotalDue);
    }

    public async Task<IEnumerable<CustomerDueDetailResponse>> GetCustomerDueDetailAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var bookings = await _repository.Query()
            .Include(b => b.BookingDetails)
            .Where(b => b.CustomerId == customerId && !b.IsDeleted && !b.IsArchived)
            .ToListAsync(cancellationToken);

        if (!bookings.Any()) return [];

        var bookingIds = bookings.Select(b => b.Id).ToList();

        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.BookingDetail)
                    .ThenInclude(bd => bd.Product)
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.DeliveryUnit)
            .Where(d => bookingIds.Contains(d.BookingId) && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var recurringChargeEntriesByBooking = await _recurringChargeEntryRepository.Query()
            .Include(e => e.BookingDetail)
                .ThenInclude(bd => bd!.Product)
            .Where(e => bookingIds.Contains(e.BookingId))
            .OrderBy(e => e.BillPeriodFrom)
            .ToListAsync(cancellationToken);

        var entriesByBookingId = recurringChargeEntriesByBooking
            .GroupBy(e => e.BookingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var payments = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                        && t.BookingId.HasValue
                        && bookingIds.Contains(t.BookingId.Value)
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && (t.TransactionHead.UsageFor == UsageFor.BILL_COLLECTION
                            || t.TransactionHead.UsageFor == UsageFor.LABOUR_CHARGE)
                            )
            .Select(t => new
            {
                BookingId = t.BookingId!.Value,
                t.DeliveryId,
                t.Amount
            })
            .ToListAsync(cancellationToken);

        var paymentsByBooking = payments
            .GroupBy(x => x.BookingId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var paymentsByDelivery = payments
            .Where(x => x.DeliveryId.HasValue)
            .GroupBy(x => x.DeliveryId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var details = new List<CustomerDueDetailResponse>();
        var now = DateTime.UtcNow;

        foreach (var booking in bookings.OrderBy(x => x.BookingDate))
        {
            var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();
            var bookingDeliveries = deliveries
                .Where(d => d.BookingId == booking.Id)
                .OrderBy(d => d.DeliveryDate)
                .ToList();

            var deliveryResponses = new List<CustomerDueDeliveryResponse>();
            decimal deliveryCharge = 0m;
            DateTime? lastDeliveryDate = null;

            if (bookingDeliveries.Any())
            {
                foreach (var delivery in bookingDeliveries)
                {
                    var labourCharge = delivery.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0m;
                    var deliveryTotal = delivery.ChargeAmount + labourCharge + delivery.AdjustmentValue;
                    var paidAmount = paymentsByDelivery.TryGetValue(delivery.Id, out var paid) ? paid : 0m;

                    deliveryResponses.Add(new CustomerDueDeliveryResponse
                    {
                        DeliveryId = delivery.Id,
                        DeliveryNumber = delivery.DeliveryNumber,
                        DeliveryDate = delivery.DeliveryDate,
                        ChargeAmount = delivery.ChargeAmount,
                        LabourCharge = labourCharge,
                        AdjustmentValue = delivery.AdjustmentValue,
                        DiscountAmount = 0,
                        PaidAmount = paidAmount,
                        DueAmount = Math.Max(deliveryTotal - paidAmount, 0m),
                        DeliveryDetails = delivery.DeliveryDetails.Select(dd => new DeliveryDetailInfoResponse
                        {
                            Id = dd.Id,
                            ProductId = dd.BookingDetail?.ProductId ?? 0,
                            ProductName = dd.BookingDetail?.Product?.ProductName ?? string.Empty,
                            DeliveryUnitId = dd.DeliveryUnitId,
                            DeliveryUnitName = dd.DeliveryUnit?.UnitName ?? string.Empty,
                            DeliveryQuantity = dd.DeliveryQuantity,
                            BaseQuantity = dd.BaseQuantity,
                            ChargeAmount = dd.ChargeAmount,
                            LabourCharge = dd.LabourCharge
                        }).ToList()
                    });
                }

                deliveryCharge = deliveryResponses.Sum(x => x.ChargeAmount + x.LabourCharge + x.AdjustmentValue);
                lastDeliveryDate = activeDetails.Count > 0
                    ? activeDetails.Max(d => (DateTime?)d.LastDeliveryDate)
                    : bookingDeliveries.Max(d => (DateTime?)d.DeliveryDate);
            }

            // Compute pending recurring charge (cycles elapsed since last delivery or booking date)
            decimal pendingRecurringCharge;
            if (lastDeliveryDate.HasValue)
            {
                pendingRecurringCharge = RecurringChargeCalculator.PendingRecurringChargeAmount(activeDetails, lastDeliveryDate.Value, now);
            }
            else
            {
                var computed = RecurringChargeCalculator.PendingRecurringChargeAmount(activeDetails, booking.BookingDate, now);
                pendingRecurringCharge = GetInitialBookingAccruedAmount(booking) + computed;
            }

            var totalAccrued = deliveryCharge + pendingRecurringCharge;
            var totalPaid = paymentsByBooking.TryGetValue(booking.Id, out var bookingPaid) ? bookingPaid : 0m;
            var totalDue = Math.Max(totalAccrued - totalPaid, 0m);
            var daysSinceBooking = (now - booking.BookingDate).Days;

            var status = "normal";
            if (totalDue > 0 && daysSinceBooking >= 30) status = "danger";
            else if (totalDue > 0 && daysSinceBooking >= 25) status = "warning";

            var bookingRecurringChargeEntries = entriesByBookingId.TryGetValue(booking.Id, out var entries) ? entries : [];

            details.Add(new CustomerDueDetailResponse
            {
                BookingId = booking.Id,
                BookingNumber = booking.BookingNumber,
                BookingDate = booking.BookingDate,
                ReferenceNumber = booking.ReferenceNumber,
                BookingLabourCharge = booking.BookingDetails.Sum(bd => bd.LabourCharge),
                OpeningBalance = 0m,         // per-booking; customer-level set at caller
                TotalAccruedAmount = totalAccrued,
                PendingRecurringChargeAmount = pendingRecurringCharge,
                LastDeliveryDate = lastDeliveryDate,
                TotalAmount = totalAccrued,  // backward-compat alias
                TotalPaid = totalPaid,
                TotalDue = totalDue,
                DaysSinceBooking = daysSinceBooking,
                Status = status,
                Deliveries = deliveryResponses,
                RecurringChargeEntries = bookingRecurringChargeEntries.Select(e => new RecurringChargeEntryResponse
                {
                    Id = (Guid)(object)e.Id!,
                    BookingId = e.BookingId,
                    BookingDetailId = e.BookingDetailId,
                    ProductName = e.BookingDetail?.Product?.ProductName ?? string.Empty,
                    RecurringChargeRunId = e.RecurringChargeRunId,
                    Source = e.Source,
                    BillPeriodFrom = e.BillPeriodFrom,
                    BillPeriodTo = e.BillPeriodTo,
                    BillType = e.BillType,
                    Cycles = e.Cycles,
                    Quantity = e.Quantity,
                    Rate = e.Rate,
                    Amount = e.Amount,
                    Note = e.Note,
                    CreatedAt = e.CreatedAt,
                }).ToList(),
            });
        }

        return details;
    }

    public async Task<CustomerOutstandingResponse> GetCustomerOutstandingAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        var bookings = await _repository.Query()
            .Include(b => b.BookingDetails)
            .Where(b => b.CustomerId == customerId && !b.IsDeleted && !b.IsArchived)
            .OrderBy(b => b.BookingDate)
            .ToListAsync(cancellationToken);

        if (!bookings.Any())
        {
            var openingOnly = customer?.OpeningBalance ?? 0m;
            return new CustomerOutstandingResponse
            {
                CustomerId = customerId,
                CustomerName = customer?.CustomerName ?? string.Empty,
                CustomerMobile = customer?.CustomerMobile ?? string.Empty,
                OpeningBalance = openingOnly,
                TotalAccrued = openingOnly,
                TotalPaid = 0m,
                TotalDue = Math.Max(openingOnly, 0m),
                Bookings = []
            };
        }

        var bookingIds = bookings.Select(b => b.Id).ToList();
        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => bookingIds.Contains(d.BookingId) && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var payments = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .Where(t => !t.IsDeleted
                        && t.BookingId.HasValue
                        && bookingIds.Contains(t.BookingId.Value)
                        && t.TransactionHead != null
                        && t.TransactionHead.Type == TransactionHeadTypes.DEBIT
                        && (t.TransactionHead.UsageFor == UsageFor.BILL_COLLECTION
                            || t.TransactionHead.UsageFor == UsageFor.LABOUR_CHARGE))
            .Select(t => new { BookingId = t.BookingId!.Value, t.Amount })
            .ToListAsync(cancellationToken);

        var paymentsByBooking = payments
            .GroupBy(x => x.BookingId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var deliveryAccruedByBooking = deliveries
            .GroupBy(d => d.BookingId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(d => d.ChargeAmount + d.AdjustmentValue + (d.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0m))
            );

        var now = DateTime.UtcNow;
        var bookingItems = new List<BookingOutstandingItem>();
        foreach (var booking in bookings)
        {
            var activeDetails = booking.BookingDetails.Where(d => !d.IsDeleted).ToList();
            decimal accrued;

            if (deliveryAccruedByBooking.TryGetValue(booking.Id, out var deliveryCharge))
            {
                // Delivered periods: use recorded delivery charges.
                // Undelivered periods since last delivery: compute dynamically.
                var lastDeliveryDate = activeDetails.Count > 0
                    ? activeDetails.Max(d => (DateTime?)d.LastDeliveryDate) ?? booking.BookingDate
                    : booking.BookingDate;

                var pendingRecurringCharge = RecurringChargeCalculator.PendingRecurringChargeAmount(activeDetails, lastDeliveryDate, now);
                accrued = deliveryCharge + pendingRecurringCharge;
            }
            else
            {
                // No deliveries yet: compute full recurring charge from booking date.
                var computed = RecurringChargeCalculator.PendingRecurringChargeAmount(activeDetails, booking.BookingDate, now);
                // Before first billing cycle completes, show initial booking charge.
                accrued = GetInitialBookingAccruedAmount(booking) + computed;
            }

            var paid = paymentsByBooking.TryGetValue(booking.Id, out var bookingPaid) ? bookingPaid : 0m;

            bookingItems.Add(new BookingOutstandingItem
            {
                BookingId = booking.Id,
                BookingNumber = booking.BookingNumber,
                BookingDate = booking.BookingDate,
                AccruedAmount = accrued,
                PaidAmount = paid,
                DueAmount = Math.Max(accrued - paid, 0m)
            });
        }

        var openingBalance = customer?.OpeningBalance ?? 0m;
        var totalAccrued = openingBalance + bookingItems.Sum(x => x.AccruedAmount);
        var totalPaid = bookingItems.Sum(x => x.PaidAmount);
        var totalDue = Math.Max(totalAccrued - totalPaid, 0m);

        return new CustomerOutstandingResponse
        {
            CustomerId = customerId,
            CustomerName = customer?.CustomerName ?? string.Empty,
            CustomerMobile = customer?.CustomerMobile ?? string.Empty,
            OpeningBalance = openingBalance,
            TotalAccrued = totalAccrued,
            TotalPaid = totalPaid,
            TotalDue = totalDue,
            Bookings = bookingItems
        };
    }

    private static decimal GetInitialBookingAccruedAmount(Booking booking)
    {
        return booking.BookingDetails.Sum(bd => ((decimal)bd.BookingQuantity * bd.BookingRate) + bd.LabourCharge);
    }
}
