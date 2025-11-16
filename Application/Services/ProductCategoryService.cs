namespace Application.Services;

public class ProductCategoryService : IProductCategoryService
{
    private readonly IRepository<ProductCategory, int> _repository;
    private readonly DefaultValueInjector _defaultValueInjector;
    public ProductCategoryService(IRepository<ProductCategory, int> repository, DefaultValueInjector defaultValueInjector)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<ProductCategoryResponse> AddAsync(ProductCategoryRequest user, CancellationToken cancellationToken = default)
    {
        ProductCategoryValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        var entity = user.Adapt<ProductCategory>();
        _defaultValueInjector.InjectCreatingAudit<ProductCategory, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<ProductCategoryResponse>() : null;
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

    public async Task<ProductCategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<ProductCategoryResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<ProductCategory, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.CategoryName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<ProductCategoryResponse> UpdateAsync(int id, ProductCategoryRequest user, CancellationToken cancellationToken = default)
    {
        ProductCategoryValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        var entity = user.Adapt(existingData);

        _defaultValueInjector.InjectUpdatingAudit<ProductCategory, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) return null;


        var response = entity.Adapt<ProductCategoryResponse>();
        return response;
    }

    public async Task<ProductCategoryResponse> ExecuteUpdateAsync(int id, ProductCategoryRequest user, CancellationToken cancellationToken = default)
    {
        ProductCategoryValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.CategoryName, user.CategoryName)
               .SetProperty(cmd => cmd.Description, user.Description)
               .SetProperty(cmd => cmd.IsActive, user.IsActive)
        );

        var response = user.Adapt<ProductCategoryResponse>();
        return response;
    }

    public async Task<IEnumerable<ProductCategoryListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new ProductCategoryListResponse(x.Id, x.CategoryName, x.Description, x.Status))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<ProductCategoryListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<ProductCategory, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.CategoryName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Description.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<ProductCategory, ProductCategoryListResponse>>? selector = x => new ProductCategoryListResponse(x.Id, x.CategoryName, x.Description, x.Status);

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

}
