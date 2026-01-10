namespace Application.Services;

public class ProductReceiveService : IProductReceiveService
{
    private readonly IRepository<Booking, Guid> _repository;
    private readonly IProductReceiveRepository _productReceiveRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public ProductReceiveService(
        IRepository<Booking, Guid> repository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        IRepository<Company, int> companyRepository,
        IStockRepository stockRepository,
        IProductReceiveRepository productReceiveRepository,
        ICodeGenerationService codeGenerationService)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _stockRepository = stockRepository;
        _productReceiveRepository = productReceiveRepository;
        _codeGenerationService = codeGenerationService;
    }

    public async Task<ProductReceiveResponse> AddAsync(ProductReceiveRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            ProductReceiveValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            ProductReceiveValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        var entity = request.Adapt<Booking>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Booking, Guid>(entity);
        if (entity.BookingDetails != null && entity.BookingDetails.Any())
        {
            _defaultValueInjector.InjectCreatingAudit<BookingDetail, Guid>(entity.BookingDetails.ToList());
        }

        var result = await _stockRepository.ManageAddProductReceiveStock(entity, cancellationToken);

        var response = result ? entity.Adapt<ProductReceiveResponse>() : throw new Exception("Failed to create product receive");
        return response;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _productReceiveRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<ProductReceiveResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<ProductReceiveResponse>() : null;
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

    public async Task<ProductReceiveResponse> UpdateAsync(Guid id, ProductReceiveRequest request, CancellationToken cancellationToken = default)
    {
        ProductReceiveValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _productReceiveRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Booking record not found!");

        existingData.BranchId = _currentUser.BranchId;
        existingData.BookingDate = request.BookingDate;
        existingData.Notes = request.Notes;

        _defaultValueInjector.InjectUpdatingAudit<Booking, Guid>(existingData);

        var response = await _productReceiveRepository.ManageUpdate(request, existingData, cancellationToken);

        return response;
    }

    public async Task<IEnumerable<ProductReceiveListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new ProductReceiveListResponse(
                x.Id,
                x.BookingNumber,
                x.BookingDate,
                x.CustomerId,
                x.Customer!,
                x.BranchId,
                x.Branch!,
                x.Notes,
                x.BookingDetails.Select(d => new ProductReceiveDetailListResponse(
                    d.Id,
                    d.Id,
                    d.ProductId,
                    d.Product!.ProductName,
                    d.BookingUnitId,
                    "",
                    d.BookingQuantity,
                    d.BookingRate,
                    d.BaseQuantity,
                    d.BaseRate))
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<ProductReceiveListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
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

        Expression<Func<Booking, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.BookingNumber.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.Customer != null && obj.Customer.CustomerName.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<Booking, ProductReceiveListResponse>>? selector = x => new ProductReceiveListResponse(
            x.Id,
            x.BookingNumber,
            x.BookingDate,
            x.CustomerId,
            x.Customer!,
            x.BranchId,
            x.Branch!,
            x.Notes,
            x.BookingDetails.Select(d => new ProductReceiveDetailListResponse(
                d.Id,
                d.Id,
                d.ProductId,
                d.Product!.ProductName,
                d.BookingUnitId,
                "",
                d.BookingQuantity,
                d.BookingRate,
                d.BaseQuantity,
                d.BaseRate))
            );

        var query = _productReceiveRepository.Query();

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateReceiveNumber(CancellationToken cancellationToken = default)
    {
        return await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "RCV",
            b => b.BookingNumber,
            cancellationToken);
    }
}
