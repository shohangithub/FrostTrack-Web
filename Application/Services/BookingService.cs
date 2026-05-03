namespace Application.Services;

public class BookingService : IBookingService
{
    private readonly IRepository<Booking, Guid> _repository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly IRepository<UnitConversion, int> _unitConversionRepository;
    private readonly IRepository<Delivery, Guid> _deliveryRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
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
        IRepository<UnitConversion, int> unitConversionRepository,
        IBookingRepository bookingRepository,
        IRepository<Delivery, Guid> deliveryRepository,
        IRepository<Transaction, Guid> transactionRepository,
        ICodeGenerationService codeGenerationService)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _unitConversionRepository = unitConversionRepository;
        _bookingRepository = bookingRepository;
        _deliveryRepository = deliveryRepository;
        _transactionRepository = transactionRepository;
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
            .Where(t => t.BookingId == id && t.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION && t.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
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
        // Get all bookings with their deliveries and transactions
        var bookings = await _repository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
            .Where(b => !b.IsArchived)
            .ToListAsync(cancellationToken);

        // Get all deliveries with their transaction info
        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
            .Where(d => !d.IsDeleted)
            .ToListAsync(cancellationToken);

        // Get all transactions for payments
        var transactions = await _transactionRepository.Query()
            .Where(t => !t.IsDeleted)
            .ToListAsync(cancellationToken);

        // Group by customer
        var customerGroups = bookings.GroupBy(b => b.CustomerId);

        var customerDueSummaries = new List<CustomerDueSummaryResponse>();

        foreach (var group in customerGroups)
        {
            var customerBookings = group.ToList();
            var customer = customerBookings.First().Customer;
            var customerId = group.Key;

            // Get all deliveries for this customer's bookings
            var bookingIds = customerBookings.Select(b => b.Id).ToList();
            var customerDeliveries = deliveries.Where(d => bookingIds.Contains(d.BookingId)).ToList();

            // Calculate totals
            decimal totalAmount = 0;
            decimal totalPaid = 0;

            foreach (var delivery in customerDeliveries)
            {
                var labourCharge = delivery.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0;
                var deliveryTotal = delivery.ChargeAmount + labourCharge + delivery.AdjustmentValue;
                totalAmount += deliveryTotal;

                // Check if this delivery has a payment transaction
                var transaction = transactions.FirstOrDefault(t => t.Id == delivery.TransactionId);
                if (transaction != null)
                {
                    totalPaid += transaction.Amount;
                }
            }

            decimal totalDue = totalAmount - totalPaid;

            // Find oldest booking
            var oldestBooking = customerBookings.OrderBy(b => b.BookingDate).First();
            var daysSinceOldestBooking = (DateTime.UtcNow - oldestBooking.BookingDate).Days;

            // Determine status
            string status = "normal";
            if (daysSinceOldestBooking >= 30)
            {
                status = "danger";
            }
            else if (daysSinceOldestBooking >= 25)
            {
                status = "warning";
            }

            customerDueSummaries.Add(new CustomerDueSummaryResponse
            {
                CustomerId = customerId,
                CustomerName = customer?.CustomerName ?? "",
                CustomerMobile = customer?.CustomerMobile ?? "",
                CustomerAddress = customer?.Address ?? "",
                TotalBookings = customerBookings.Count,
                TotalAmount = totalAmount,
                TotalPaid = totalPaid,
                TotalDue = totalDue,
                OldestBookingDate = oldestBooking.BookingDate,
                DaysSinceOldestBooking = daysSinceOldestBooking,
                Status = status
            });
        }

        return customerDueSummaries.OrderByDescending(c => c.TotalDue);
    }

    public async Task<IEnumerable<CustomerDueDetailResponse>> GetCustomerDueDetailAsync(int customerId, CancellationToken cancellationToken = default)
    {
        // Get all bookings for this customer
        var bookings = await _repository.Query()
            .Include(b => b.Customer)
            .Include(b => b.BookingDetails)
            .Where(b => b.CustomerId == customerId && !b.IsArchived)
            .ToListAsync(cancellationToken);

        // Get all deliveries for these bookings
        var bookingIds = bookings.Select(b => b.Id).ToList();
        var deliveries = await _deliveryRepository.Query()
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.BookingDetail)
                    .ThenInclude(bd => bd.Product)
            .Include(d => d.DeliveryDetails)
                .ThenInclude(dd => dd.DeliveryUnit)
            .Where(d => bookingIds.Contains(d.BookingId) && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        // Get all transactions for payments
        var transactionIds = deliveries.Where(d => d.TransactionId.HasValue)
            .Select(d => d.TransactionId!.Value)
            .ToList();

        var transactions = await _transactionRepository.Query()
            .Where(t => transactionIds.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var customerDueDetails = new List<CustomerDueDetailResponse>();

        foreach (var booking in bookings)
        {
            var bookingDeliveries = deliveries.Where(d => d.BookingId == booking.Id).ToList();

            decimal totalAmount = 0;
            decimal totalPaid = 0;

            var deliveryResponses = new List<CustomerDueDeliveryResponse>();

            foreach (var delivery in bookingDeliveries)
            {
                var labourCharge = delivery.DeliveryDetails?.Sum(dd => dd.LabourCharge) ?? 0;
                var deliveryTotal = delivery.ChargeAmount + labourCharge + delivery.AdjustmentValue;
                totalAmount += deliveryTotal;

                decimal paidAmount = 0;
                if (delivery.TransactionId.HasValue)
                {
                    var transaction = transactions.FirstOrDefault(t => t.Id == delivery.TransactionId);
                    if (transaction != null)
                    {
                        paidAmount = transaction.Amount;
                        totalPaid += paidAmount;
                    }
                }

                decimal deliveryDue = deliveryTotal - paidAmount;

                deliveryResponses.Add(new CustomerDueDeliveryResponse
                {
                    DeliveryId = delivery.Id,
                    DeliveryNumber = delivery.DeliveryNumber,
                    DeliveryDate = delivery.DeliveryDate,
                    ChargeAmount = delivery.ChargeAmount,
                    LabourCharge = labourCharge,
                    AdjustmentValue = delivery.AdjustmentValue,
                    DiscountAmount = 0, // Add if you have discount field
                    PaidAmount = paidAmount,
                    DueAmount = deliveryDue,
                    DeliveryDetails = delivery.DeliveryDetails.Select(dd => new DeliveryDetailInfoResponse
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
                });
            }

            var daysSinceBooking = (DateTime.UtcNow - booking.BookingDate).Days;

            // Determine status
            string status = "normal";
            if (daysSinceBooking >= 30)
            {
                status = "danger";
            }
            else if (daysSinceBooking >= 25)
            {
                status = "warning";
            }

            customerDueDetails.Add(new CustomerDueDetailResponse
            {
                BookingId = booking.Id,
                BookingNumber = booking.BookingNumber,
                BookingDate = booking.BookingDate,
                ReferenceNumber = booking.ReferenceNumber,
                TotalAmount = totalAmount,
                TotalPaid = totalPaid,
                TotalDue = totalAmount - totalPaid,
                DaysSinceBooking = daysSinceBooking,
                Status = status,
                Deliveries = deliveryResponses
            });
        }

        return customerDueDetails.OrderBy(d => d.BookingDate);
    }
}

