namespace Application.Services;

using Application.Validators.PaymentMethod;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly IRepository<PaymentMethod, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public PaymentMethodService(IRepository<PaymentMethod, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<PaymentMethodResponse> AddAsync(PaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            PaymentMethodValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            PaymentMethodValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        var entity = request.Adapt<PaymentMethod>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<PaymentMethod, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<PaymentMethodResponse>() : throw new InvalidOperationException("Failed to create payment method");
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

    public async Task<PaymentMethodResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result?.Adapt<PaymentMethodResponse>();
        return response ?? throw new ArgumentException($"Payment Method with ID {id} not found");
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<PaymentMethod, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.MethodName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PaymentMethodListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MethodName)
            .ToListAsync(cancellationToken);
        return result.Adapt<IEnumerable<PaymentMethodListResponse>>();
    }

    public async Task<IEnumerable<PaymentMethodListResponse>> GetActiveListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MethodName)
            .ToListAsync(cancellationToken);
        return result.Adapt<IEnumerable<PaymentMethodListResponse>>();
    }

    public async Task<IEnumerable<PaymentMethodListResponse>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(x => x.IsActive && x.Category.ToLower() == category.ToLower())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MethodName)
            .ToListAsync(cancellationToken);
        return result.Adapt<IEnumerable<PaymentMethodListResponse>>();
    }

    public async Task<PaymentMethodResponse> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        var response = result?.Adapt<PaymentMethodResponse>();
        return response ?? throw new ArgumentException($"Payment Method with Code {code} not found");
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            var maxId = await _repository.Query().Where(x => x.BranchId == _currentUser.BranchId).MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"PAY-{_currentUser.BranchId:D3}-{maxId + 1:D6}";
        }
        else
        {
            var maxId = await _repository.Query().MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"PAY-{maxId + 1:D6}";
        }
    }

    public async Task<PaginationResult<PaymentMethodListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<PaymentMethod, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.MethodName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.Description != null && obj.Description.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Code != null && obj.Code.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<PaymentMethod, PaymentMethodListResponse>>? selector = x => new PaymentMethodListResponse(
            x.Id,
            x.MethodName,
            x.Code,
            x.Description,
            x.Category,
            x.RequiresBankAccount,
            x.RequiresCheckDetails,
            x.RequiresOnlineDetails,
            x.RequiresMobileWalletDetails,
            x.RequiresCardDetails,
            x.IsActive,
            x.SortOrder,
            x.IconClass,
            x.BranchId,
            x.Status
        );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<PaymentMethodResponse> UpdateAsync(int id, PaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        PaymentMethodValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingEntity = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingEntity is null) throw new ArgumentException($"Payment Method with ID {id} not found");

        request.Adapt(existingEntity);
        _defaultValueInjector.InjectUpdatingAudit<PaymentMethod, int>(existingEntity);
        var result = await _repository.UpdateAsync(existingEntity, cancellationToken);
        if (result is null) throw new InvalidOperationException("Failed to update payment method");

        var response = existingEntity.Adapt<PaymentMethodResponse>();
        return response;
    }
}