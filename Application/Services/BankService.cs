namespace Application.Services;

public class BankService : IBankService
{
    private readonly IRepository<Bank, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public BankService(IRepository<Bank, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<BankResponse> AddAsync(BankRequest bank, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BankValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(bank, cancellationToken);
        }
        else
        {
            BankValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(bank, cancellationToken);
        }

        var entity = bank.Adapt<Bank>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Bank, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<BankResponse>() : throw new InvalidOperationException("Failed to create bank");
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
            return $"BNK-{_currentUser.BranchId:D3}-{maxId + 1:D6}";
        }
        else
        {
            var maxId = await _repository.Query().MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"BNK-{maxId + 1:D6}";
        }
    }

    public async Task<BankResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result?.Adapt<BankResponse>();
        return response ?? throw new ArgumentException($"Bank with ID {id} not found");
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Bank, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.BankName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<BankListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .OrderBy(x => x.BankName)
            .ToListAsync(cancellationToken);
        var response = result.Adapt<IEnumerable<BankListResponse>>();
        return response;
    }

    public async Task<PaginationResult<BankListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<Bank, bool>>? predicate = x => true;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.BankName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.BankCode.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.AccountNumber != null && obj.AccountNumber.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.AccountTitle != null && obj.AccountTitle.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.SwiftCode != null && obj.SwiftCode.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.ContactPerson != null && obj.ContactPerson.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<Bank, BankListResponse>>? selector = x => new BankListResponse(
               x.Id,
               x.BankName,
               x.BankCode,
               x.BankBranch,
               x.AccountNumber,
               x.AccountTitle,
               x.SwiftCode,
               x.RoutingNumber,
               x.IBANNumber,
               x.ContactPerson,
               x.ContactPhone,
               x.ContactEmail,
               x.Address,
               x.OpeningBalance,
               x.CurrentBalance,
               x.Description,
               x.IsMainAccount,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<BankResponse> UpdateAsync(int id, BankRequest bank, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BankValidator validator = new(_repository, id, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(bank, cancellationToken);
        }
        else
        {
            BankValidator validator = new(_repository, id);
            await validator.ValidateAndThrowAsync(bank, cancellationToken);
        }

        var entity = bank.Adapt(existingData);
        _defaultValueInjector.InjectUpdatingAudit<Bank, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) throw new InvalidOperationException("Failed to update bank");

        var response = entity.Adapt<BankResponse>();
        return response;
    }

    public async Task<decimal> GetCurrentBalanceAsync(int bankId, CancellationToken cancellationToken = default)
    {
        var bank = await _repository.GetByIdAsync(bankId, cancellationToken);
        if (bank is null) throw new ArgumentNullException(nameof(bank), "Bank not found");
        return bank.CurrentBalance;
    }
}