namespace Application.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IRepository<Delivery, Guid> _repository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<BookingDetail, Guid> _bookingDetailRepository;
    private readonly IRepository<DeliveryDetail, Guid> _detailRepository;
    private readonly IRepository<UnitConversion, int> _unitConversionRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly ITransactionService _transactionService;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public DeliveryService(
        IRepository<Delivery, Guid> repository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<BookingDetail, Guid> bookingDetailRepository,
        IRepository<DeliveryDetail, Guid> detailRepository,
        IRepository<UnitConversion, int> unitConversionRepository,
        IRepository<Transaction, Guid> transactionRepository,
        ITransactionService transactionService,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService)
    {
        _repository = repository;
        _bookingRepository = bookingRepository;
        _bookingDetailRepository = bookingDetailRepository;
        _detailRepository = detailRepository;
        _unitConversionRepository = unitConversionRepository;
        _transactionRepository = transactionRepository;
        _transactionService = transactionService;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<DeliveryResponse> CreateAsync(CreateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        // Validate stock availability
        await ValidateStockAvailability(request);

        var entity = request.Adapt<Delivery>();
        entity.BranchId = _currentUser.BranchId;
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

        await _repository.AddAsync(entity, CancellationToken.None);

        // Create transaction if requested
        if (request.CreateTransaction && request.TransactionAmount.HasValue && request.TransactionAmount.Value > 0)
        {
            var booking = await _bookingRepository.Query()
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId);

            var transactionRequest = new TransactionRequest(
                Id: Guid.NewGuid(),
                TransactionCode: await _transactionService.GenerateTransactionCode(),
                TransactionDate: request.DeliveryDate,
                TransactionType: TransactionTypes.BILL_COLLECTION,
                TransactionFlow: TransactionFlows.IN,
                EntityName: "Delivery",
                EntityId: entity.Id.ToString(),
                BranchId: entity.BranchId,
                CustomerId: booking?.CustomerId,
                BookingId: request.BookingId,
                Amount: request.TransactionAmount.Value,
                DiscountAmount: 0,
                AdjustmentValue: 0,
                NetAmount: request.TransactionAmount.Value,
                PaymentMethod: request.PaymentMethod ?? PaymentMethods.CASH,
                PaymentReference: null,
                Category: null,
                SubCategory: null,
                Description: $"Payment for Delivery {entity.DeliveryNumber}",
                Note: request.TransactionNotes,
                VendorName: null,
                VendorContact: null,
                BillingPeriodStart: null,
                BillingPeriodEnd: null,
                AttachmentPath: null
            );

            await _transactionService.AddAsync(transactionRequest, CancellationToken.None);
        }

        return await GetByIdAsync(entity.Id);
    }

    public async Task<DeliveryResponse> UpdateAsync(Guid id, UpdateDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.Query()
            .Include(x => x.DeliveryDetails)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existing == null)
            throw new Exception("Product delivery not found");

        // Validate stock availability (considering current delivery)
        await ValidateStockAvailability(request, id);

        request.Adapt(existing);
        _defaultValueInjector.InjectUpdatingAudit<Delivery, Guid>(existing);

        // Remove old details using DeletableQuery
        await _detailRepository.DeletableQuery(x => x.DeliveryId == id).ExecuteDeleteAsync();

        if (request.DeliveryDetails != null && request.DeliveryDetails.Any())
        {
            existing.DeliveryDetails = request.DeliveryDetails.Select(d => new DeliveryDetail
            {
                DeliveryId = existing.Id,
                BookingDetailId = d.BookingDetailId,
                DeliveryUnitId = d.DeliveryUnitId,
                DeliveryQuantity = d.DeliveryQuantity,
                BaseQuantity = d.BaseQuantity,
                ChargeAmount = d.ChargeAmount,
                AdjustmentValue = d.AdjustmentValue
            }).ToList();
            _defaultValueInjector.InjectCreatingAudit<DeliveryDetail, Guid>(existing.DeliveryDetails.ToList());
        }

        await _repository.UpdateAsync(existing, CancellationToken.None);

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await _repository.DeletableQuery(x => x.Id == id).ExecuteDeleteAsync();
        return result > 0;
    }

    public async Task<bool> BatchDeleteAsync(Guid[] ids)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        return result > 0;
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

        return entity.Adapt<DeliveryResponse>();
    }

    public async Task<PaginationResult<DeliveryResponse>> GetWithPaginationAsync(PaginationQuery query)
    {
        var baseQuery = _repository.Query().AsQueryable();

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
                AdjustmentValue = d.AdjustmentValue
            }).ToList()
        });

        return await PaginationResult<DeliveryResponse>.CreateAsync(
            projectedQuery,
            query.PageIndex,
            query.PageSize);
    }


    public async Task<string> GenerateDeliveryNumberAsync()
    {
        var lastNumber = await _repository.Query()
            .OrderByDescending(x => x.Id)
            .Select(x => x.DeliveryNumber)
            .FirstOrDefaultAsync();

        return GenerateNextNumber(lastNumber, "DEL");
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
                .FirstOrDefaultAsync(bd => bd.Id == detail.BookingDetailId);

            if (bookingDetail == null)
                throw new Exception($"Booking detail not found");

            // Calculate already delivered quantity for this booking detail
            var deliveredQty = await _detailRepository.Query()
                .Where(dd => dd.BookingDetailId == detail.BookingDetailId && dd.DeliveryId != existingDeliveryId)
                .SumAsync(dd => dd.DeliveryQuantity);

            var availableStock = bookingDetail.BookingQuantity - deliveredQty;

            if (detail.DeliveryQuantity > availableStock)
                throw new Exception($"Insufficient stock for product. Available: {availableStock}, Requested: {detail.DeliveryQuantity}");
        }
    }

    private async Task ValidateStockAvailability(UpdateDeliveryRequest request, Guid? existingDeliveryId = null)
    {
        foreach (var detail in request.DeliveryDetails)
        {
            var bookingDetail = await _bookingDetailRepository.Query()
                .FirstOrDefaultAsync(bd => bd.Id == detail.BookingDetailId);

            if (bookingDetail == null)
                throw new Exception($"Booking detail not found");

            // Calculate already delivered quantity for this booking detail
            var deliveredQty = await _detailRepository.Query()
                .Where(dd => dd.BookingDetailId == detail.BookingDetailId && dd.DeliveryId != existingDeliveryId)
                .SumAsync(dd => dd.DeliveryQuantity);

            var availableStock = bookingDetail.BookingQuantity - deliveredQty;

            if (detail.DeliveryQuantity > availableStock)
                throw new Exception($"Insufficient stock for product. Available: {availableStock}, Requested: {detail.DeliveryQuantity}");
        }
    }

    private string GenerateNextNumber(string? lastNumber, string prefix)
    {
        if (string.IsNullOrEmpty(lastNumber))
            return $"{prefix}-{DateTime.Now.Year}-0001";

        var parts = lastNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int number))
        {
            return $"{prefix}-{DateTime.Now.Year}-{(number + 1):D4}";
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
        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .Include(b => b.Branch)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Product)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.BookingUnit)
            .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.DeliveryDetails)
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);

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
            // Calculate total delivered for this booking detail
            var totalDelivered = detail.DeliveryDetails.Sum(dd => dd.DeliveryQuantity);

            var remainingQty = detail.BookingQuantity - totalDelivered;



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
            .Where(bd => bd.Booking != null && bd.Booking.Id == bookingId)
            .ToListAsync();

        var deliveries = await _repository.Query()
            .Where(d => d.BookingId == bookingId)
            .Include(d => d.DeliveryDetails)
            .ToListAsync();

        var results = new List<RemainingQuantityResponse>();

        foreach (var detail in bookingDetails)
        {
            var totalDelivered = deliveries
                .SelectMany(d => d.DeliveryDetails)
                .Where(dd => dd.BookingDetailId == detail.Id)
                .Sum(dd => dd.DeliveryQuantity);

            results.Add(new RemainingQuantityResponse
            {
                BookingDetailId = detail.Id,
                BookingQuantity = detail.BookingQuantity,
                TotalDeliveredQuantity = totalDelivered,
                RemainingQuantity = detail.BookingQuantity - totalDelivered
            });
        }

        return results;
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetBookingLookupAsync()
    {
        return await _bookingRepository.Query()
            .Where(b => b.TenantId == _tenantId)
            .OrderByDescending(b => b.CreatedTime)
            .Select(b => new Lookup<Guid>(b.Id, b.BookingNumber))
            .ToListAsync();
    }

    public async Task<decimal> GetBookingPreviousPaymentsAsync(Guid bookingId)
    {
        // Get all deliveries for this booking
        var deliveries = await _repository.Query()
            .Where(d => d.BookingId == bookingId)
            .Select(d => d.Id.ToString())
            .ToListAsync();

        if (!deliveries.Any())
            return 0;

        // Sum up all bill collection transactions for these deliveries
        var totalPaid = await _transactionRepository.Query()
            .Where(t => t.TransactionType == "BILL_COLLECTION"
                     && t.EntityName == "Delivery"
                     && deliveries.Contains(t.EntityId))
            .SumAsync(t => t.Amount);

        return totalPaid;
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
            BookingRate = d.BookingDetail?.BookingRate ?? 0
        }).ToList();

        // Calculate Total Paid Amount (from all transactions for this booking)
        response.TotalPaidAmount = await GetBookingPreviousPaymentsAsync(entity.BookingId);

        // Calculate Extra Charge (total charge amount from all deliveries for this booking)
        var allDeliveries = await _repository.Query()
            .Where(d => d.BookingId == entity.BookingId)
            .ToListAsync();

        var totalDeliveryCharges = allDeliveries.Sum(d => d.ChargeAmount + d.AdjustmentValue);
        response.ExtraCharge = totalDeliveryCharges - response.TotalBookingAmount;

        // Calculate Due Amount
        response.DueAmount = (response.TotalBookingAmount + response.ExtraCharge) - response.TotalPaidAmount;

        return response;
    }
}