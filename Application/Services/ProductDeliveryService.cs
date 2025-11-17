namespace Application.Services;

public class ProductDeliveryService : IProductDeliveryService
{
    private readonly IRepository<ProductDelivery, long> _repository;
    private readonly IRepository<ProductReceive, long> _productReceiveRepository;
    private readonly IRepository<ProductDeliveryDetail, long> _detailRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public ProductDeliveryService(
        IRepository<ProductDelivery, long> repository,
        IRepository<ProductReceive, long> productReceiveRepository,
        IRepository<ProductDeliveryDetail, long> detailRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService)
    {
        _repository = repository;
        _productReceiveRepository = productReceiveRepository;
        _detailRepository = detailRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<ProductDeliveryResponse> CreateAsync(ProductDeliveryRequest request)
    {
        // Validate stock availability
        await ValidateStockAvailability(request);

        var entity = request.Adapt<ProductDelivery>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<ProductDelivery, long>(entity);

        if (entity.ProductDeliveryDetails != null && entity.ProductDeliveryDetails.Any())
        {
            foreach (var detail in entity.ProductDeliveryDetails)
            {
                detail.CreatedTime = DateTime.Now;
            }
        }

        await _repository.AddAsync(entity, CancellationToken.None);

        return await GetByIdAsync((int)entity.Id);
    }

    public async Task<ProductDeliveryResponse> UpdateAsync(int id, ProductDeliveryRequest request)
    {
        var existing = await _repository.Query()
            .Include(x => x.ProductDeliveryDetails)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existing == null)
            throw new Exception("Product delivery not found");

        // Validate stock availability (considering current delivery)
        await ValidateStockAvailability(request, id);

        request.Adapt(existing);
        _defaultValueInjector.InjectUpdatingAudit<ProductDelivery, long>(existing);

        // Remove old details using DeletableQuery
        await _detailRepository.DeletableQuery(x => x.ProductDeliveryId == id).ExecuteDeleteAsync();

        if (request.ProductDeliveryDetails != null && request.ProductDeliveryDetails.Any())
        {
            existing.ProductDeliveryDetails = request.ProductDeliveryDetails.Select(d => new ProductDeliveryDetail
            {
                ProductDeliveryId = existing.Id,
                ProductId = d.ProductId,
                DeliveryUnitId = d.DeliveryUnitId,
                DeliveryQuantity = d.DeliveryQuantity,
                BookingRate = d.BookingRate,
                DeliveryAmount = d.DeliveryAmount,
                CreatedTime = DateTime.Now
            }).ToList();
        }

        await _repository.UpdateAsync(existing, CancellationToken.None);

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _repository.DeletableQuery(x => x.Id == id).ExecuteDeleteAsync();
        return result > 0;
    }

    public async Task<bool> BatchDeleteAsync(int[] ids)
    {
        var longIds = ids.Select(x => (long)x).ToList();
        var result = await _repository.DeletableQuery(x => longIds.Contains(x.Id)).ExecuteDeleteAsync();
        return result > 0;
    }

    public async Task<ProductDeliveryResponse> GetByIdAsync(int id)
    {
        var entity = await _repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.ProductDeliveryDetails)
                .ThenInclude(d => d.Product)
            .Include(x => x.ProductDeliveryDetails)
                .ThenInclude(d => d.DeliveryUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            throw new Exception("Product delivery not found");

        return entity.Adapt<ProductDeliveryResponse>();
    }

    public async Task<PaginationResult<ProductDeliveryListResponse>> GetWithPaginationAsync(PaginationQuery query)
    {
        var queryable = _repository.Query()
            .Include(x => x.Customer)
            .Include(x => x.ProductDeliveryDetails)
                .ThenInclude(d => d.Product)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(query.OpenText))
        {
            queryable = queryable.Where(x =>
                x.DeliveryNumber.Contains(query.OpenText) ||
                x.Customer.CustomerName.Contains(query.OpenText));
        }

        // Apply ordering
        queryable = ApplyOrdering(queryable, query.OrderBy, query.IsAscending);

        var result = await PaginationResult<ProductDelivery>.CreateAsync(
            queryable,
            query.PageIndex,
            query.PageSize);

        var data = result.Data.Select(x => x.Adapt<ProductDeliveryListResponse>()).ToList();

        return await PaginationResult<ProductDeliveryListResponse>.CreateAsync(
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
        // Get all receives for this customer with product details
        var receives = await _productReceiveRepository.Query()
            .Where(pr => pr.CustomerId == customerId)
            .Include(pr => pr.ProductReceiveDetails)
                .ThenInclude(prd => prd.Product)
            .Include(pr => pr.ProductReceiveDetails)
                .ThenInclude(prd => prd.ReceiveUnit)
            .ToListAsync();

        // Get all deliveries for this customer
        var deliveries = await _repository.Query()
            .Where(d => d.CustomerId == customerId)
            .Include(d => d.ProductDeliveryDetails)
            .ToListAsync();

        // Calculate stock per product
        var stockDictionary = new Dictionary<int, CustomerStockResponse>();

        foreach (var receive in receives)
        {
            foreach (var detail in receive.ProductReceiveDetails)
            {
                var key = detail.ProductId;
                if (!stockDictionary.ContainsKey(key))
                {
                    stockDictionary[key] = new CustomerStockResponse
                    {
                        CustomerId = customerId,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product.ProductName,
                        UnitId = detail.ReceiveUnitId,
                        UnitName = detail.ReceiveUnit.UnitName,
                        AvailableStock = 0,
                        BookingRate = detail.BookingRate
                    };
                }
                stockDictionary[key] = stockDictionary[key] with
                {
                    AvailableStock = stockDictionary[key].AvailableStock + (decimal)detail.ReceiveQuantity,
                    BookingRate = Math.Max(stockDictionary[key].BookingRate, (decimal)detail.BookingRate)
                };
            }
        }

        // Subtract deliveries
        foreach (var delivery in deliveries)
        {
            foreach (var detail in delivery.ProductDeliveryDetails)
            {
                if (stockDictionary.ContainsKey(detail.ProductId))
                {
                    stockDictionary[detail.ProductId] = stockDictionary[detail.ProductId] with
                    {
                        AvailableStock = stockDictionary[detail.ProductId].AvailableStock - detail.DeliveryQuantity
                    };
                }
            }
        }

        // Return only products with available stock > 0
        return stockDictionary.Values
            .Where(x => x.AvailableStock > 0)
            .ToList();
    }

    private async Task ValidateStockAvailability(ProductDeliveryRequest request, int? existingDeliveryId = null)
    {
        var customerStock = await GetCustomerStockAsync(request.CustomerId);

        foreach (var detail in request.ProductDeliveryDetails)
        {
            var stock = customerStock.FirstOrDefault(s => s.ProductId == detail.ProductId);

            if (stock == null)
                throw new Exception($"Product not found in customer stock");

            var availableStock = stock.AvailableStock;

            // If updating, add back the current delivery quantity
            if (existingDeliveryId.HasValue)
            {
                var currentDetail = await _detailRepository.Query()
                    .Where(x => x.ProductDeliveryId == existingDeliveryId.Value && x.ProductId == detail.ProductId)
                    .FirstOrDefaultAsync();

                if (currentDetail != null)
                    availableStock += currentDetail.DeliveryQuantity;
            }

            if (detail.DeliveryQuantity > availableStock)
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

    private IQueryable<ProductDelivery> ApplyOrdering(IQueryable<ProductDelivery> queryable, string? orderBy, bool? isAscending)
    {
        Expression<Func<ProductDelivery, object>> keySelector = orderBy?.ToLower() switch
        {
            "deliverynumber" => x => x.DeliveryNumber,
            "deliverydate" => x => x.DeliveryDate,
            "totalamount" => x => x.TotalAmount,
            _ => x => x.Id
        };

        return isAscending == true ? queryable.OrderBy(keySelector) : queryable.OrderByDescending(keySelector);
    }
}
