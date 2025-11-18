namespace Application.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IRepository<Delivery, Guid> _repository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<BookingDetail, Guid> _bookingDetailRepository;
    private readonly IRepository<DeliveryDetail, Guid> _detailRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public DeliveryService(
        IRepository<Delivery, Guid> repository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<BookingDetail, Guid> bookingDetailRepository,
        IRepository<DeliveryDetail, Guid> detailRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService)
    {
        _repository = repository;
        _bookingRepository = bookingRepository;
        _bookingDetailRepository = bookingDetailRepository;
        _detailRepository = detailRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<DeliveryResponse> CreateAsync(DeliveryRequest request)
    {
        // Validate stock availability
        await ValidateStockAvailability(request);

        var entity = request.Adapt<Delivery>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Delivery, Guid>(entity);

        if (entity.DeliveryDetails != null && entity.DeliveryDetails.Any())
        {
            _defaultValueInjector.InjectCreatingAudit<DeliveryDetail, Guid>(entity.DeliveryDetails.ToList());
        }

        await _repository.AddAsync(entity, CancellationToken.None);

        return await GetByIdAsync(entity.Id);
    }

    public async Task<DeliveryResponse> UpdateAsync(Guid id, DeliveryRequest request)
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
                .ThenInclude(b => b!.Customer)
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

    public async Task<PaginationResult<DeliveryListResponse>> GetWithPaginationAsync(PaginationQuery query)
    {
        var queryable = _repository.Query()
            .Include(x => x.Booking)
                .ThenInclude(b => b!.Customer)
            .Include(x => x.DeliveryDetails)
                .ThenInclude(d => d.BookingDetail)
                    .ThenInclude(bd => bd!.Product)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(query.OpenText))
        {
            queryable = queryable.Where(x =>
                x.DeliveryNumber.Contains(query.OpenText) ||
                (x.Booking != null && x.Booking.BookingNumber.Contains(query.OpenText)) ||
                (x.Booking != null && x.Booking.Customer != null && x.Booking.Customer.CustomerName.Contains(query.OpenText)));
        }

        // Apply ordering
        queryable = ApplyOrdering(queryable, query.OrderBy, query.IsAscending);

        var result = await PaginationResult<Delivery>.CreateAsync(
            queryable,
            query.PageIndex,
            query.PageSize);

        var data = result.Data.Select(x => new DeliveryListResponse
        {
            Id = x.Id,
            DeliveryNumber = x.DeliveryNumber,
            DeliveryDate = x.DeliveryDate,
            BookingId = x.BookingId,
            BookingNumber = x.Booking?.BookingNumber ?? "",
            CustomerId = x.Booking?.CustomerId ?? 0,
            CustomerName = x.Booking?.Customer?.CustomerName ?? "",
            ChargeAmount = x.ChargeAmount,
            DiscountAmount = x.DiscountAmount,
            PaidAmount = x.PaidAmount,
            DeliveryDetails = new List<DeliveryDetailResponse>()
        }).ToList();

        return await PaginationResult<DeliveryListResponse>.CreateAsync(
            data.AsQueryable(),
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

    private async Task ValidateStockAvailability(DeliveryRequest request, Guid? existingDeliveryId = null)
    {
        // Get the booking to find customer
        var booking = await _bookingRepository.Query()
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null)
            throw new Exception("Booking not found");

        var customerStock = await GetCustomerStockAsync(booking.CustomerId);

        foreach (var detail in request.DeliveryDetails)
        {
            var stock = customerStock.FirstOrDefault(s => s.BookingDetailId == detail.BookingDetailId);

            if (stock == null)
                throw new Exception($"Booking detail not found in customer stock");

            var availableStock = stock.AvailableStock;

            // If updating, add back the current delivery quantity
            if (existingDeliveryId.HasValue)
            {
                var currentDetail = await _detailRepository.Query()
                    .Where(x => x.DeliveryId == existingDeliveryId.Value && x.BookingDetailId == detail.BookingDetailId)
                    .FirstOrDefaultAsync();

                if (currentDetail != null)
                    availableStock += (decimal)currentDetail.DeliveryQuantity;
            }

            if ((decimal)detail.DeliveryQuantity > availableStock)
                throw new Exception($"Insufficient stock for product. Available: {availableStock}");
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
}
