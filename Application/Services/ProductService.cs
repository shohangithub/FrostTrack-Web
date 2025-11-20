namespace Application.Services;

public class ProductService : IProductService
{
    private readonly IRepository<Product, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;
    public ProductService(IRepository<Product, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<ProductResponse> AddAsync(ProductRequest product, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            ProductValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(product, cancellationToken);
        }
        else
        {
            ProductValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(product, cancellationToken);
        }


        var entity = product.Adapt<Product>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Product, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<ProductResponse>() : null;
        return response;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));
        return await _repository.DeleteAsync(existingData, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<ProductResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.ProductName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<ProductResponse> UpdateAsync(int id, ProductRequest product, CancellationToken cancellationToken = default)
    {
        ProductValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(product, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) return null;

        var existingProductCode = existingData.ProductCode;
        var entity = product.Adapt(existingData);
        entity.ProductCode = existingProductCode;
        _defaultValueInjector.InjectUpdatingAudit<Product, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) return null;


        var response = entity.Adapt<ProductResponse>();
        return response;
    }

    public async Task<ProductResponse> ExecuteUpdateAsync(int id, ProductRequest product, CancellationToken cancellationToken = default)
    {
        ProductValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(product, cancellationToken);

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.CategoryId, product.CategoryId)
               .SetProperty(cmd => cmd.CustomBarcode, product.CustomBarcode)
               .SetProperty(cmd => cmd.DefaultUnitId, product.DefaultUnitId)
               .SetProperty(cmd => cmd.ImageUrl, product.ImageUrl)
               .SetProperty(cmd => cmd.IsActive, product.IsActive)
               .SetProperty(cmd => cmd.ProductName, product.ProductName)
               .SetProperty(cmd => cmd.BookingRate, product.BookingRate)
        );

        var response = product.Adapt<ProductResponse>();
        return response;
    }

    public async Task<IEnumerable<ProductListResponse>> ListAsync(Expression<Func<Product, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _repository.Query();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var response = await query
           .Select(x => new ProductListResponse(
               x.Id,
               x.ProductName,
               x.ProductCode,
               x.CustomBarcode,
               x.CategoryId,
               x.Category.CategoryName,
               x.DefaultUnitId,
               x.DefaultUnit.UnitName,
               x.ImageUrl,
               x.BookingRate,
               x.Status
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<IEnumerable<ProductLisWithStockResponse>> ListwithStockAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query().Include(x => x.Category).Include(x => x.DefaultUnit).Include(x => x.Stock).ThenInclude(x => x.UnitConversion)
           .Select(x => new ProductLisWithStockResponse(
               x.Id,
               x.ProductName,
               x.ProductCode,
               x.CategoryId,
               x.Category.CategoryName,
               x.DefaultUnitId,
               x.DefaultUnit.UnitName,
               x.ImageUrl,
               x.BookingRate,
               x.Status,
               x.Stock.StockQuantity,
               x.Stock.LastPurchaseRate,
               x.Stock.UnitConversion
               )).ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<ProductListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<Product, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.ProductName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.ProductCode.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Category.CategoryName.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<Product, ProductListResponse>>? selector = x => new ProductListResponse(
               x.Id,
               x.ProductName,
               x.ProductCode,
               x.CustomBarcode,
               x.CategoryId,
               x.Category.CategoryName,
               x.DefaultUnitId,
               x.DefaultUnit.UnitName,
               x.ImageUrl,
               x.BookingRate,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (dependOn == ECodeGeneration.Branch)
        {
            var code = int.Parse((await _repository.Query().Where(x => x.BranchId == _currentUser.BranchId).OrderByDescending(x => x.ProductCode).Select(x => x.ProductCode).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 2) ?? "0") + 1;
            var range = code / 10;

            if (range == 0)
                return $"P-0000{code}";//P-00099
            else if (range <= 9)
                return $"P-000{code}";//P-00099
            else if (range <= 99)
                return $"P-00{code}"; //P-00999
            else if (range <= 999)
                return $"P-0{code}"; //P-09999
            else
                return $"P-{code}"; //P-99999
        }
        else
        {
            var code = int.Parse((await _repository.Query().OrderByDescending(x => x.ProductCode).Select(x => x.ProductCode).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 2) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"P-0000{code}";//P-00099
            else if (range <= 9)
                return $"P-000{code}";//P-00099
            else if (range <= 99)
                return $"P-00{code}"; //P-00999
            else if (range <= 999)
                return $"P-0{code}"; //P-09999
            else
                return $"P-{code}"; //P-99999
        }
    }

}
