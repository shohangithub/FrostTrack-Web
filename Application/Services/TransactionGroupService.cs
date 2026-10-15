using Application.Common;
using Application.Contractors;

namespace Application.Services;

public class TransactionGroupService : ITransactionGroupService
{
    private readonly IRepository<TransactionGroup, Guid> _repository;
    private readonly IRepository<TransactionHead, Guid> _headRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;

    public TransactionGroupService(
        IRepository<TransactionGroup, Guid> repository,
        IRepository<TransactionHead, Guid> headRepository,
        ICodeGenerationService codeGenerationService,
        DefaultValueInjector defaultValueInjector)
    {
        _repository = repository;
        _headRepository = headRepository;
        _codeGenerationService = codeGenerationService;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<TransactionGroupResponse?> AddAsync(TransactionGroupRequest request, CancellationToken cancellationToken = default)
    {
        TransactionGroupValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = request.Adapt<TransactionGroup>();
        entity.Name = request.Name.Trim();
        entity.AccountType = string.IsNullOrWhiteSpace(request.AccountType) ? null : request.AccountType.Trim();
        entity.Code = await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "TG",
            g => g.Code,
            cancellationToken);
        _defaultValueInjector.InjectCreatingAudit<TransactionGroup, Guid>(entity);

        var result = await _repository.AddAsync(entity, cancellationToken);
        return result ? entity.Adapt<TransactionGroupResponse>() : null;
    }

    public async Task<TransactionGroupResponse?> UpdateAsync(Guid id, TransactionGroupRequest request, CancellationToken cancellationToken = default)
    {
        TransactionGroupValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null)
            throw new KeyNotFoundException($"Transaction group with ID {id} not found");

        existingData.Name = request.Name.Trim();
        existingData.SortOrder = request.SortOrder;
        existingData.Description = request.Description;
        existingData.IsActive = request.IsActive;
        existingData.AccountType = string.IsNullOrWhiteSpace(request.AccountType) ? null : request.AccountType.Trim();

        _defaultValueInjector.InjectUpdatingAudit<TransactionGroup, Guid>(existingData);
        var result = await _repository.UpdateAsync(existingData, cancellationToken);

        if (result is null) return null;
        return existingData.Adapt<TransactionGroupResponse>();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DeleteTransactionGroupValidator validator = new(_repository, _headRepository);
        await validator.ValidateAndThrowAsync(id, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        return await _repository.DeleteAsync(existingData, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        // Skip groups still mapped to a transaction head
        var usedIds = await _headRepository.Query()
            .Where(x => x.TransactionGroupId != null && ids.Contains(x.TransactionGroupId.Value))
            .Select(x => x.TransactionGroupId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (usedIds.Count > 0)
            throw new BusinessRuleException("Some selected groups are mapped to transaction heads. Remove the mapping first.");

        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<TransactionGroupResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        return result?.Adapt<TransactionGroupResponse>();
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(CancellationToken cancellationToken = default)
    {
        return await _repository.Query()
            .Where(x => x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new Lookup<Guid>(x.Id, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginationResult<TransactionGroupListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<TransactionGroup, bool>>? predicate = x => !x.IsDeleted;

        if (!string.IsNullOrEmpty(requestQuery.OpenText))
        {
            var text = requestQuery.OpenText;
            predicate = x =>
                !x.IsDeleted &&
                (x.Code.Contains(text) ||
                 x.Name.Contains(text) ||
                 (x.Description != null && x.Description.Contains(text)));
        }

        var heads = _headRepository.Query();
        Expression<Func<TransactionGroup, TransactionGroupListResponse>>? selector = x => new TransactionGroupListResponse(
            x.Id!,
            x.Code,
            x.Name,
            x.SortOrder,
            heads.Count(h => h.TransactionGroupId == x.Id && !h.IsDeleted),
            x.IsActive ? "Active" : "Inactive",
            x.AccountType
        );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }
}
