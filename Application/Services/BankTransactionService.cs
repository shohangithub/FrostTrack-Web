namespace Application.Services;

public class BankTransactionService : IBankTransactionService
{
    private readonly IRepository<BankTransaction, long> _repository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public BankTransactionService(
        IRepository<BankTransaction, long> repository,
        IRepository<Bank, int> bankRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _bankRepository = bankRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<BankTransactionResponse> AddAsync(BankTransactionRequest bankTransaction, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BankTransactionValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(bankTransaction, cancellationToken);
        }
        else
        {
            BankTransactionValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(bankTransaction, cancellationToken);
        }

        // Get current bank balance
        var bank = await _bankRepository.GetByIdAsync(bankTransaction.BankId, cancellationToken);
        if (bank is null) throw new ArgumentException("Bank not found");

        var entity = bankTransaction.Adapt<BankTransaction>();
        entity.BranchId = _currentUser.BranchId;

        // Calculate new balance
        if (bankTransaction.TransactionType == "Deposit")
        {
            entity.BalanceAfter = bank.CurrentBalance + bankTransaction.Amount;
            bank.CurrentBalance += bankTransaction.Amount;
        }
        else if (bankTransaction.TransactionType == "Withdraw")
        {
            if (bank.CurrentBalance < bankTransaction.Amount)
                throw new InvalidOperationException("Insufficient balance for withdrawal");

            entity.BalanceAfter = bank.CurrentBalance - bankTransaction.Amount;
            bank.CurrentBalance -= bankTransaction.Amount;
        }

        _defaultValueInjector.InjectCreatingAudit<BankTransaction, long>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);

        // Update bank balance
        await _bankRepository.UpdateAsync(bank, cancellationToken);

        var response = result ? entity.Adapt<BankTransactionResponse>() : throw new InvalidOperationException("Failed to create bank transaction");
        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));
        return await _repository.DeleteAsync(existingData, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default)
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
            return $"TXN-{_currentUser.BranchId:D3}-{maxId + 1:D6}";
        }
        else
        {
            var maxId = await _repository.Query().MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;
            return $"TXN-{maxId + 1:D6}";
        }
    }

    public async Task<BankTransactionResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.Bank)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (result is null) throw new ArgumentException($"Bank Transaction with ID {id} not found");

        var response = new BankTransactionResponse(
            result.Id,
            result.TransactionNumber,
            result.TransactionDate,
            result.BankId,
            result.Bank.BankName,
            result.TransactionType,
            result.Amount,
            result.Reference,
            result.Description,
            result.BalanceAfter,
            result.ReceiptNumber,
            result.IsActive,
            result.Status
        );

        return response;
    }

    public async Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<BankTransaction, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(predicate)
            .Select(x => new Lookup<long>(x.Id, x.TransactionNumber))
            .OrderBy(x => x.Text)
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<BankTransactionListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.Bank)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken);

        var response = result.Select(x => new BankTransactionListResponse(
            x.Id,
            x.TransactionNumber,
            x.TransactionDate,
            x.BankId,
            x.Bank.BankName,
            x.TransactionType,
            x.Amount,
            x.Reference,
            x.Description,
            x.BalanceAfter,
            x.ReceiptNumber,
            x.Status
        ));

        return response;
    }

    public async Task<PaginationResult<BankTransactionListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<BankTransaction, bool>>? predicate = x => true;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.TransactionNumber.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Bank.BankName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.TransactionType.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.Reference != null && obj.Reference.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Description != null && obj.Description.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<BankTransaction, BankTransactionListResponse>>? selector = x => new BankTransactionListResponse(
               x.Id,
               x.TransactionNumber,
               x.TransactionDate,
               x.BankId,
               x.Bank.BankName,
               x.TransactionType,
               x.Amount,
               x.Reference,
               x.Description,
               x.BalanceAfter,
               x.ReceiptNumber,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<BankTransactionResponse> UpdateAsync(long id, BankTransactionRequest bankTransaction, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BankTransactionValidator validator = new(_repository, id, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(bankTransaction, cancellationToken);
        }
        else
        {
            BankTransactionValidator validator = new(_repository, id);
            await validator.ValidateAndThrowAsync(bankTransaction, cancellationToken);
        }

        var entity = bankTransaction.Adapt(existingData);
        _defaultValueInjector.InjectUpdatingAudit<BankTransaction, long>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) throw new InvalidOperationException("Failed to update bank transaction");

        var response = entity.Adapt<BankTransactionResponse>();
        return response;
    }
}