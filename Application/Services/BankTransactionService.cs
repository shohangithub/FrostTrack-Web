namespace Application.Services;

public class BankTransactionService : IBankTransactionService
{
    private readonly IRepository<BankTransaction, long> _repository;
    private readonly IRepository<Bank, int> _bankRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly ICodeGenerationService _codeGenerationService;
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
        IRepository<Company, int> companyRepository,
        ICodeGenerationService codeGenerationService)
    {
        _repository = repository;
        _bankRepository = bankRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _codeGenerationService = codeGenerationService;
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
        entity.TransactionDate = DateTime.UtcNow;

        // Calculate new balance
        if (bankTransaction.TransactionType == BankTransactionTypes.Deposit)
        {
            entity.BalanceAfter = bank.CurrentBalance + bankTransaction.Amount;
            bank.CurrentBalance += bankTransaction.Amount;
        }
        else if (bankTransaction.TransactionType == BankTransactionTypes.Withdraw)
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

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        return await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "BT",
            bt => bt.TransactionNumber,
            cancellationToken);
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
            result.IsDeleted,
            result.IsArchived,
            result.DeletedAt,
            result.ArchivedAt,
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
            x.IsDeleted,
            x.IsArchived,
            x.DeletedAt,
            x.ArchivedAt,
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
             x.IsDeleted,
             x.IsArchived,
             x.DeletedAt,
             x.ArchivedAt,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<PaginationResult<BankTransactionListResponse>> PaginationListAsync(BankTransactionPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        // Map frontend column names to entity property names
        if (!string.IsNullOrEmpty(requestQuery.OrderBy))
        {
            var mappedOrderBy = requestQuery.OrderBy switch
            {
                "transactionNumber" => nameof(BankTransaction.TransactionNumber),
                "transactionDate" => nameof(BankTransaction.TransactionDate),
                "bankName" => nameof(BankTransaction.BankId),
                "transactionType" => nameof(BankTransaction.TransactionType),
                "amount" => nameof(BankTransaction.Amount),
                "reference" => nameof(BankTransaction.Reference),
                "description" => nameof(BankTransaction.Description),
                "balanceAfter" => nameof(BankTransaction.BalanceAfter),
                "receiptNumber" => nameof(BankTransaction.ReceiptNumber),
                "status" => nameof(BankTransaction.IsActive),
                _ => requestQuery.OrderBy
            };
            requestQuery = requestQuery with { OrderBy = mappedOrderBy };
        }

        var archiveStatus = requestQuery.archiveStatus?.ToLowerInvariant() ?? "active";
        Expression<Func<BankTransaction, bool>> predicate = x => true;

        predicate = archiveStatus switch
        {
            "archived" => predicate.And(x => !x.IsDeleted && x.IsArchived),
            "deleted" => predicate.And(x => x.IsDeleted && x.TenantId == _tenantId),
            _ => predicate.And(x => !x.IsDeleted && !x.IsArchived)
        };

        // Filter by Transaction Type
        if (!string.IsNullOrWhiteSpace(requestQuery.transactionType))
        {
            predicate = predicate.And(x => x.TransactionType == requestQuery.transactionType);
        }

        // Filter by Status
        if (!string.IsNullOrWhiteSpace(requestQuery.status))
        {
            bool isActive = requestQuery.status.ToLower() == "active";
            predicate = predicate.And(x => x.IsActive == isActive);
        }

        // Filter by Date Range (ignore time)
        if (requestQuery.DateFrom.HasValue)
        {
            var fromLocal = requestQuery.DateFrom.Value
         .ToDateTime(TimeOnly.MinValue);

            var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local)
                .ToUniversalTime();

            predicate = predicate.And(x => x.TransactionDate >= fromUtc);
        }

        if (requestQuery.DateTo.HasValue)
        {
            var toLocalExclusive = requestQuery.DateTo.Value
          .AddDays(1)
          .ToDateTime(TimeOnly.MinValue);

            var toUtcExclusive = DateTime.SpecifyKind(toLocalExclusive, DateTimeKind.Local)
                .ToUniversalTime();

            predicate = predicate.And(x => x.TransactionDate < toUtcExclusive);
        }

        // Filter by Open Text Search
        if (!string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchText = requestQuery.OpenText.Trim().ToLower();
            predicate = predicate.And(obj =>
                (obj.TransactionNumber != null && obj.TransactionNumber.ToLower().Contains(searchText)) ||
                (obj.Bank.BankName != null && obj.Bank.BankName.ToLower().Contains(searchText)) ||
                (obj.TransactionType != null && obj.TransactionType.ToLower().Contains(searchText)) ||
                (obj.Reference != null && obj.Reference.ToLower().Contains(searchText)) ||
                (obj.Description != null && obj.Description.ToLower().Contains(searchText)) ||
                obj.Amount.ToString().Contains(searchText)
            );
        }

        Expression<Func<BankTransaction, BankTransactionListResponse>> selector = x => new BankTransactionListResponse(
            x.Id,
            x.TransactionNumber,
            x.TransactionDate,
            x.BankId,
            x.Bank.BankName,
            x.TransactionType,
            x.TransactionType == BankTransactionTypes.Deposit ? x.Amount : -x.Amount,
            x.Reference,
            x.Description,
            x.BalanceAfter,
            x.ReceiptNumber,
            x.IsDeleted,
            x.IsArchived,
            x.DeletedAt,
            x.ArchivedAt,
            x.Status
        );

        var baseQuery = archiveStatus == "deleted"
            ? _repository.UnfilteredQuery()
            : _repository.Query();

        var query = baseQuery
            .Include(x => x.Bank)
            .AsQueryable();

        // Apply search predicate
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
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