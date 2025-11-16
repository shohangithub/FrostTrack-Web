namespace Application.Services;

public class AssetService : IAssetService
{
    private readonly IRepository<Asset, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public AssetService(IRepository<Asset, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<AssetResponse> AddAsync(AssetRequest asset, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            AssetValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(asset, cancellationToken);
        }
        else
        {
            AssetValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(asset, cancellationToken);
        }

        var entity = asset.Adapt<Asset>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Asset, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<AssetResponse>() : throw new InvalidOperationException("Failed to create asset");
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
        var existingData = await _repository.Query().Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        if (existingData is null || !existingData.Any()) throw new ArgumentNullException(nameof(existingData));

        foreach (var entity in existingData)
        {
            await _repository.DeleteAsync(entity, cancellationToken);
        }
        return true;
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            var maxId = await _repository.Query().Where(x => x.BranchId == _currentUser.BranchId).MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"AST-{_currentUser.BranchId:D3}-{maxId + 1:D6}";
        }
        else
        {
            var maxId = await _repository.Query().MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"AST-{maxId + 1:D6}";
        }
    }

    public async Task<AssetResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        if (result is null) throw new ArgumentNullException(nameof(result));
        return result.Adapt<AssetResponse>();
    }

    public async Task<decimal> GetCurrentValueAsync(int assetId, CancellationToken cancellationToken = default)
    {
        var asset = await _repository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null) throw new ArgumentNullException(nameof(asset));
        return asset.CurrentValue;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Asset, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var query = _repository.Query().Where(predicate);
        var result = await query.Select(x => new Lookup<int> { Value = x.Id, Text = x.AssetName }).ToListAsync(cancellationToken);
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AssetListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().ToListAsync(cancellationToken);
        return result.Adapt<IEnumerable<AssetListResponse>>();
    }

    public async Task<PaginationResult<AssetListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<Asset, bool>>? predicate = x => true;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.AssetName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.AssetCode.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.AssetType != null && obj.AssetType.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.SerialNumber != null && obj.SerialNumber.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Model != null && obj.Model.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Manufacturer != null && obj.Manufacturer.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Location != null && obj.Location.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Department != null && obj.Department.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.AssignedTo != null && obj.AssignedTo.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<Asset, AssetListResponse>>? selector = x => new AssetListResponse(
               x.Id,
               x.AssetName,
               x.AssetCode,
               x.AssetType,
               x.SerialNumber,
               x.Model,
               x.Manufacturer,
               x.PurchaseDate,
               x.PurchaseCost,
               x.CurrentValue,
               x.DepreciationRate,
               x.Location,
               x.Department,
               x.AssignedTo,
               x.Condition,
               x.WarrantyExpiryDate,
               x.MaintenanceDate,
               x.Notes,
               x.ImageUrl,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<AssetResponse> UpdateAsync(int id, AssetRequest asset, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            AssetValidator validator = new(_repository, id, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(asset, cancellationToken);
        }
        else
        {
            AssetValidator validator = new(_repository, id);
            await validator.ValidateAndThrowAsync(asset, cancellationToken);
        }

        var entity = asset.Adapt(existingData);
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectUpdatingAudit<Asset, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) throw new InvalidOperationException("Failed to update asset");

        var response = entity.Adapt<AssetResponse>();
        return response;
    }

    public async Task<IEnumerable<string>> GetDistinctAssetTypesAsync(CancellationToken cancellationToken = default)
    {
        var distinctTypes = await _repository.Query()
            .Where(x => !string.IsNullOrEmpty(x.AssetType))
            .Select(x => x.AssetType!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return distinctTypes;
    }
}