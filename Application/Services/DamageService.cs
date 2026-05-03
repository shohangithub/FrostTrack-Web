namespace Application.Services;

public class DamageService : IDamageService
{
    private readonly IRepository<Damage, int> _repository;
    private readonly IStockRepository _stockRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public DamageService(IRepository<Damage, int> repository, IStockRepository stockRepository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _stockRepository = stockRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<DamageResponse> AddAsync(DamageRequest damage, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            DamageValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(damage, cancellationToken);
        }
        else
        {
            DamageValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(damage, cancellationToken);
        }

        var entity = damage.Adapt<Damage>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Damage, int>(entity);

        var result = await _stockRepository.ManageAddDamageStock(entity, cancellationToken);
        var response = result ? entity.Adapt<DamageResponse>() : throw new InvalidOperationException("Failed to create damage");
        return response;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));
        return await _stockRepository.ManageDeleteDamageStock(existingData, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.Query().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (!entities.Any()) return false;
        return await _stockRepository.ManageBatchDeleteDamageStock(entities, cancellationToken);
    }

    public async Task<DamageResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var damage = await _repository.GetByIdAsync(id, cancellationToken);
        return damage?.Adapt<DamageResponse>() ?? throw new ArgumentNullException(nameof(damage));
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Damage, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        return await _repository.Query()
            .Where(x => x.TenantId == tenantId)
            .Where(predicate)
            .Select(x => new Lookup<int>(x.Id, x.DamageNumber))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken) is not null;
    }

    public async Task<IEnumerable<DamageListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        return await _repository.Query()
            .Where(x => x.TenantId == tenantId)
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Select(x => new DamageListResponse(
                x.Id, x.DamageNumber, x.DamageDate, x.Product.ProductName, x.Unit.UnitName,
                x.Quantity, x.UnitCost, x.TotalCost, x.Reason, x.Status,
                x.IsDeleted, x.IsArchived, x.DeletedAt, x.ArchivedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginationResult<DamageListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var branchId = _currentUser.BranchId;

        Expression<Func<Damage, bool>>? predicate;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchText = requestQuery.OpenText.ToLower();
            predicate = x => x.TenantId == tenantId && x.BranchId == branchId && !x.IsDeleted && !x.IsArchived &&
                (x.DamageNumber.ToLower().Contains(searchText) ||
                 x.Product.ProductName.ToLower().Contains(searchText) ||
                 (x.Reason != null && x.Reason.ToLower().Contains(searchText)));
        }
        else
        {
            predicate = x => x.TenantId == tenantId && x.BranchId == branchId && !x.IsDeleted && !x.IsArchived;
        }

        Expression<Func<Damage, DamageListResponse>> selector = x => new DamageListResponse(
            x.Id, x.DamageNumber, x.DamageDate, x.Product.ProductName, x.Unit.UnitName,
            x.Quantity, x.UnitCost, x.TotalCost, x.Reason, x.Status,
            x.IsDeleted, x.IsArchived, x.DeletedAt, x.ArchivedAt
        );

        var result = await _repository.PaginationQuery(
            paginationQuery: requestQuery,
            predicate: predicate,
            selector: selector,
            cancellationToken
        );

        return result;
    }

    public async Task<DamageResponse> UpdateAsync(int id, DamageRequest damage, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            DamageValidator validator = new(_repository, id, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(damage, cancellationToken);
        }
        else
        {
            DamageValidator validator = new(_repository, id);
            await validator.ValidateAndThrowAsync(damage, cancellationToken);
        }

        var entity = damage.Adapt(existingData);
        _defaultValueInjector.InjectUpdatingAudit<Damage, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) throw new InvalidOperationException("Failed to update damage");

        var response = entity.Adapt<DamageResponse>();
        return response;
    }

    public async Task<PaginationResult<DamageListResponse>> PaginationListAsync(SetupPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var archiveStatus = requestQuery.status?.ToLowerInvariant() ?? "active";

        Expression<Func<Damage, bool>> predicate = archiveStatus switch
        {
            "archived" => x => x.TenantId == tenantId && !x.IsDeleted && x.IsArchived,
            "deleted" => x => x.TenantId == tenantId && x.IsDeleted,
            _ => x => x.TenantId == tenantId && !x.IsDeleted && !x.IsArchived
        };

        if (!string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var search = requestQuery.OpenText.Trim().ToLower();
            predicate = predicate.And(obj =>
                obj.DamageNumber.ToLower().Contains(search) ||
                obj.Product.ProductName.ToLower().Contains(search));
        }

        Expression<Func<Damage, DamageListResponse>> selector = x => new DamageListResponse(
            x.Id, x.DamageNumber, x.DamageDate, x.Product.ProductName, x.Unit.UnitName,
            x.Quantity, x.UnitCost, x.TotalCost, x.Reason, x.Status,
            x.IsDeleted, x.IsArchived, x.DeletedAt, x.ArchivedAt
        );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        var tenantId = _tenantProvider.GetTenantId();

        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            var maxId = await _repository.Query().Where(x => x.TenantId == tenantId && x.BranchId == _currentUser.BranchId).MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"DMG-{_currentUser.BranchId:D3}-{maxId + 1:D6}";
        }
        else
        {
            var maxId = await _repository.Query().Where(x => x.TenantId == tenantId).MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"DMG-{maxId + 1:D6}";
        }
    }
}
