namespace Application.Services;

public class ProductReceiveService : IProductReceiveService
{
    private readonly IRepository<Booking, Guid> _repository;
    private readonly IProductReceiveRepository _productReceiveRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IRepository<Company, int> _companyRepository;
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
        IProductReceiveRepository productReceiveRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _stockRepository = stockRepository;
        _productReceiveRepository = productReceiveRepository;
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
        var currentDate = DateTime.Now;
        var year = currentDate.Year.ToString()?.Remove(0, 2);
        var month = currentDate.Month / 10 == 0 ? "0" + currentDate.Month : currentDate.Month.ToString();
        var dateString = $"{year}{month}";
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();

        if (dependOn == ECodeGeneration.Branch)
        {
            var code = long.Parse((await _repository.Query()
                .Where(x => x.BranchId == _currentUser.BranchId && x.BookingDate.Month == currentDate.Month)
                .OrderByDescending(x => x.BookingNumber)
                .Select(x => x.BookingNumber)
                .FirstOrDefaultAsync(cancellationToken))?.Remove(0, 5) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"B{dateString}0000{code}";
            else if (range <= 9)
                return $"B{dateString}000{code}";
            else if (range <= 99)
                return $"B{dateString}00{code}";
            else if (range <= 999)
                return $"B{dateString}0{code}";
            else
                return $"B{dateString}{code}";
        }
        else
        {
            var code = long.Parse((await _repository.Query()
                .OrderByDescending(x => x.BookingNumber)
                .Select(x => x.BookingNumber)
                .FirstOrDefaultAsync(cancellationToken))?.Remove(0, 5) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"B{dateString}0000{code}";
            else if (range <= 9)
                return $"B{dateString}000{code}";
            else if (range <= 99)
                return $"B{dateString}00{code}";
            else if (range <= 999)
                return $"B{dateString}0{code}";
            else
                return $"B{dateString}{code}";
        }
    }
}
