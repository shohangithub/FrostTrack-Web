using Application.Contractors;

namespace Application.Services;

public class TransactionHeadService : ITransactionHeadService
{
    private readonly IRepository<TransactionHead, Guid> _repository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;

    public TransactionHeadService(
        IRepository<TransactionHead, Guid> repository,
        ICodeGenerationService codeGenerationService,
        DefaultValueInjector defaultValueInjector)
    {
        _repository = repository;
        _codeGenerationService = codeGenerationService;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<TransactionHeadResponse> AddAsync(TransactionHeadRequest request, CancellationToken cancellationToken = default)
    {
        TransactionHeadValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = request.Adapt<TransactionHead>();
        entity.Code = await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "TH",
            th => th.Code,
            cancellationToken);
        entity.UsageFor = UsageFor.TRANSACTION;
        entity.IsSystem = false; // User-created heads are not system heads
        _defaultValueInjector.InjectCreatingAudit<TransactionHead, Guid>(entity);

        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<TransactionHeadResponse>() : null;
        return response;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DeleteTransactionHeadValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(id, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        return await _repository.DeleteAsync(existingData, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        // Only delete non-system heads
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id) && !x.IsSystem)
            .ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<TransactionHeadResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<TransactionHeadResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<TransactionHead, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(predicate)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new Lookup<Guid>(x.Id, x.Name))
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<IEnumerable<TransactionHeadLookup>> GetTransactionLookup(CancellationToken cancellationToken = default)
    {
        return await _repository.Query()
            .Where(x => x.UsageFor == UsageFor.TRANSACTION && x.IsActive)
            .Select(x => new TransactionHeadLookup(
                x.Id,
                x.Name,
                !string.IsNullOrEmpty(x.DisplayType) ? x.DisplayType : x.Type
            )).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<TransactionHeadListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return result.Adapt<IEnumerable<TransactionHeadListResponse>>();
    }

    public async Task<PaginationResult<TransactionHeadListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<TransactionHead, bool>>? predicate = x => x.UsageFor == UsageFor.TRANSACTION;

        // Search filter
        if (!string.IsNullOrEmpty(requestQuery.OpenText))
        {
            predicate = x =>
                x.Code.Contains(requestQuery.OpenText) ||
                x.Name.Contains(requestQuery.OpenText) ||
                (x.Description != null && x.Description.Contains(requestQuery.OpenText));
        }

        Expression<Func<TransactionHead, TransactionHeadListResponse>>? selector = x => new TransactionHeadListResponse(
            x.Id,
            x.Code,
            x.Name,
            x.Type,
            x.DisplayType,
            x.IsSystem,
            x.IsActive ? "Active" : "Inactive"
        );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<TransactionHeadResponse> UpdateAsync(Guid id, TransactionHeadRequest request, CancellationToken cancellationToken = default)
    {
        TransactionHeadValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null)
            throw new KeyNotFoundException($"Transaction head with ID {id} not found");

        // Prevent modifying system heads' critical properties
        if (existingData.IsSystem)
        {
            existingData.Name = request.Name;
            existingData.Description = request.Description;
            existingData.SortOrder = request.SortOrder;
            existingData.ColorCode = request.ColorCode;
            existingData.IconClass = request.IconClass;
            existingData.IsActive = request.IsActive;
        }
        else
        {
            var entity = request.Adapt(existingData);
            entity.UsageFor = UsageFor.TRANSACTION;
            existingData = entity;
        }

        _defaultValueInjector.InjectUpdatingAudit<TransactionHead, Guid>(existingData);
        var result = await _repository.UpdateAsync(existingData, cancellationToken);

        if (result is null) return null;

        var response = existingData.Adapt<TransactionHeadResponse>();
        return response;
    }
}
