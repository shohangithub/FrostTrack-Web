namespace Application.Services;

public class UnitConversionService : IUnitConversionService
{
    private readonly IRepository<UnitConversion, int> _repository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly CurrentUser _currentUser;
    public UnitConversionService(IRepository<UnitConversion, int> repository, DefaultValueInjector defaultValueInjector, IUserContextService userContextService)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<UnitConversionResponse> AddAsync(UnitConversionRequest user, CancellationToken cancellationToken = default)
    {
        UnitConversionValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        var entity = user.Adapt<UnitConversion>();
        _defaultValueInjector.InjectCreatingAudit<UnitConversion, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<UnitConversionResponse>() : null;
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

    public async Task<UnitConversionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<UnitConversionResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<UnitConversion, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.UnitName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<UnitConversionResponse> UpdateAsync(int id, UnitConversionRequest user, CancellationToken cancellationToken = default)
    {
        UnitConversionValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        var entity = user.Adapt(existingData);

        _defaultValueInjector.InjectUpdatingAudit<UnitConversion, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) return null;


        var response = entity.Adapt<UnitConversionResponse>();
        return response;
    }

    public async Task<UnitConversionResponse> ExecuteUpdateAsync(int id, UnitConversionRequest user, CancellationToken cancellationToken = default)
    {
        UnitConversionValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.UnitName, user.UnitName)
               .SetProperty(cmd => cmd.Description, user.Description)
               .SetProperty(cmd => cmd.IsActive, user.IsActive)
        );

        var response = user.Adapt<UnitConversionResponse>();
        return response;
    }

    public async Task<IEnumerable<UnitConversionListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query().Include(x => x.BaseUnit)
           .Select(x => new UnitConversionListResponse(x.Id, x.UnitName, x.BaseUnit.Id, x.BaseUnit.UnitName, x.ConversionValue, x.Description, x.Status, x.IsDeleted, x.IsArchived, x.DeletedAt, x.ArchivedAt))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<UnitConversionListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<UnitConversion, bool>>? predicate = x => !x.IsDeleted && !x.IsArchived;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => !obj.IsDeleted && !obj.IsArchived
                            && (obj.UnitName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Description.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<UnitConversion, UnitConversionListResponse>> selector = x => new UnitConversionListResponse(x.Id, x.UnitName, x.BaseUnit.Id, x.BaseUnit.UnitName, x.ConversionValue, x.Description, x.Status, x.IsDeleted, x.IsArchived, x.DeletedAt, x.ArchivedAt);

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<PaginationResult<UnitConversionListResponse>> PaginationListAsync(SetupPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        var archiveStatus = requestQuery.status?.ToLowerInvariant() ?? "active";

        Expression<Func<UnitConversion, bool>> predicate = archiveStatus switch
        {
            "archived" => x => !x.IsDeleted && x.IsArchived,
            "deleted" => x => x.IsDeleted,
            _ => x => !x.IsDeleted && !x.IsArchived
        };

        if (!string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var search = requestQuery.OpenText.Trim().ToLower();
            predicate = predicate.And(obj => obj.UnitName.ToLower().Contains(search));
        }

        Expression<Func<UnitConversion, UnitConversionListResponse>> selector = x => new UnitConversionListResponse(x.Id, x.UnitName, x.BaseUnit.Id, x.BaseUnit.UnitName, x.ConversionValue, x.Description, x.Status, x.IsDeleted, x.IsArchived, x.DeletedAt, x.ArchivedAt);

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

}
