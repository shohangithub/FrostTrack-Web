namespace Application.Services;

public class BaseUnitService : IBaseUnitService
{
    private readonly IRepository<BaseUnit, int> _repository;
    private readonly IRepository<UnitConversion, int> _unitConversionRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    public BaseUnitService(IRepository<BaseUnit, int> repository, DefaultValueInjector defaultValueInjector, IRepository<UnitConversion, int> unitConversionRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _unitConversionRepository = unitConversionRepository;
    }

    public async Task<BaseUnitResponse> AddAsync(BaseUnitRequest user, CancellationToken cancellationToken = default)
    {
        BaseUnitValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        var entity = user.Adapt<BaseUnit>();
        _defaultValueInjector.InjectCreatingAudit<BaseUnit, int>(entity);

        var conversionEntity = entity.Adapt<UnitConversion>();
        conversionEntity.Id = 0;
        conversionEntity.BaseUnit = entity;
        conversionEntity.ConversionValue = 1;
        //  var result = await _repository.AddAsync(entity, cancellationToken);
        var conversionResult = await _unitConversionRepository.AddAsync(conversionEntity, cancellationToken);
        var response = conversionResult ? entity.Adapt<BaseUnitResponse>() : null;
        return response;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        DeleteBaseUnitValidator validator = new(_repository, _unitConversionRepository, id);
        await validator.ValidateAndThrowAsync(id, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));
        var isDeleted = await _unitConversionRepository.DeletableQuery(x => x.BaseUnit.Id == existingData.Id).ExecuteDeleteAsync(cancellationToken);
        if (isDeleted > 0)
        {
            return await _repository.DeleteAsync(existingData, cancellationToken);
        }
        return false;
    }

    public async Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<BaseUnitResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<BaseUnitResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<BaseUnit, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.UnitName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<BaseUnitResponse> UpdateAsync(int id, BaseUnitRequest unit, CancellationToken cancellationToken = default)
    {
        BaseUnitValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(unit, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        var entity = unit.Adapt(existingData);

        _defaultValueInjector.InjectUpdatingAudit<BaseUnit, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);


        var updateConversion = _unitConversionRepository.UpdatableQuery(x => x.BaseUnit.Id == id && x.ConversionValue == 1).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.UnitName, unit.UnitName)
               .SetProperty(cmd => cmd.Description, unit.Description)
               .SetProperty(cmd => cmd.IsActive, unit.IsActive)
               .SetProperty(cmd => cmd.LastUpdatedById, entity.LastUpdatedById)
               .SetProperty(cmd => cmd.LastUpdatedTime, entity.LastUpdatedTime)
        );

        if (result is null || updateConversion <= 0) return null;


        var response = entity.Adapt<BaseUnitResponse>();
        return response;
    }

    public async Task<BaseUnitResponse> ExecuteUpdateAsync(int id, BaseUnitRequest user, CancellationToken cancellationToken = default)
    {
        BaseUnitValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(user, cancellationToken);

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.UnitName, user.UnitName)
               .SetProperty(cmd => cmd.Description, user.Description)
               .SetProperty(cmd => cmd.IsActive, user.IsActive)
        );

        var response = user.Adapt<BaseUnitResponse>();
        return response;
    }

    public async Task<IEnumerable<BaseUnitListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new BaseUnitListResponse(x.Id, x.UnitName, x.Description, x.Status))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<BaseUnitListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<BaseUnit, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.UnitName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Description.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<BaseUnit, BaseUnitListResponse>>? selector = x => new BaseUnitListResponse(x.Id, x.UnitName, x.Description, x.Status);

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

}
