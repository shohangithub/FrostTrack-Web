namespace Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IRepository<Transaction, Guid> _repository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public TransactionService(
        IRepository<Transaction, Guid> repository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<TransactionResponse> AddAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        TransactionValidator validator = new(_repository);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = request.Adapt<Transaction>();
        entity.BranchId = _currentUser.BranchId;

        // Set default PaymentMethod to CASH if not provided
        if (string.IsNullOrEmpty(entity.PaymentMethod))
        {
            entity.PaymentMethod = PaymentMethods.CASH;
        }

        // Set default EntityName if not provided
        if (string.IsNullOrEmpty(entity.EntityName))
        {
            entity.EntityName = "GENERAL";
        }

        // Set default EntityId if not provided
        if (string.IsNullOrEmpty(entity.EntityId))
        {
            entity.EntityId = "00000000-0000-0000-0000-000000000000";
        }

        // Set default Description if empty
        if (string.IsNullOrEmpty(entity.Description))
        {
            entity.Description = $"{entity.TransactionType} - {entity.TransactionFlow}";
        }

        // Make amount negative for OUT transactions
        if (entity.TransactionFlow == TransactionFlows.OUT && entity.Amount > 0)
        {
            entity.Amount = -entity.Amount;
        }

        // Calculate NetAmount
        entity.NetAmount = entity.Amount - entity.DiscountAmount + entity.AdjustmentValue;

        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(entity);

        await _repository.AddAsync(entity, cancellationToken);

        var response = entity.Adapt<TransactionResponse>();
        return response;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        return await _repository.DeleteAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<TransactionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        var response = result is not null ? result.Adapt<TransactionResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<Guid>>> GetLookup(Expression<Func<Transaction, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Where(predicate)
            .Select(x => new Lookup<Guid>(x.Id, x.TransactionCode))
            .ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<TransactionResponse> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default)
    {
        TransactionValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        request.Adapt(entity);
        entity.BranchId = _currentUser.BranchId;

        // Set default PaymentMethod to CASH if not provided
        if (string.IsNullOrEmpty(entity.PaymentMethod))
        {
            entity.PaymentMethod = PaymentMethods.CASH;
        }

        // Make amount negative for OUT transactions
        if (entity.TransactionFlow == TransactionFlows.OUT && entity.Amount > 0)
        {
            entity.Amount = -entity.Amount;
        }

        // Calculate NetAmount
        entity.NetAmount = entity.Amount - entity.DiscountAmount + entity.AdjustmentValue;

        _defaultValueInjector.InjectUpdatingAudit<Transaction, Guid>(entity);

        await _repository.UpdateAsync(entity, cancellationToken);

        var response = entity.Adapt<TransactionResponse>();
        return response;
    }

    public async Task<IEnumerable<TransactionListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Select(x => new TransactionListResponse(
                x.Id,
                x.TransactionCode,
                x.TransactionDate,
                x.TransactionType,
                x.TransactionFlow,
                x.BranchId,
                x.Branch!.Name,
                x.CustomerId,
                x.Customer != null ? x.Customer.CustomerName : null,
                x.NetAmount,
                x.PaymentMethod,
                x.Category,
                x.Description,
                x.VendorName
            ))
            .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<TransactionListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<Transaction, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.TransactionCode.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Description.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.VendorName != null && obj.VendorName.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Customer != null && obj.Customer.CustomerName.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<Transaction, TransactionListResponse>>? selector = x => new TransactionListResponse(
            x.Id,
            x.TransactionCode,
            x.TransactionDate,
            x.TransactionType,
            x.TransactionFlow,
            x.BranchId,
            x.Branch!.Name,
            x.CustomerId,
            x.Customer != null ? x.Customer.CustomerName : null,
            x.NetAmount,
            x.PaymentMethod,
            x.Category,
            x.Description,
            x.VendorName
        );

        var query = _repository.Query()
            .Include(x => x.Branch)
            .Include(x => x.Customer);

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateTransactionCode(CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.Now;
        var year = currentDate.Year.ToString().Substring(2, 2);
        var month = currentDate.Month.ToString("D2");
        var dateString = $"{year}{month}";

        var lastTransaction = await _repository.Query()
            .Where(x => x.TransactionDate.Year == currentDate.Year && x.TransactionDate.Month == currentDate.Month)
            .OrderByDescending(x => x.TransactionCode)
            .Select(x => x.TransactionCode)
            .FirstOrDefaultAsync(cancellationToken);

        long code = 1;
        if (!string.IsNullOrEmpty(lastTransaction) && lastTransaction.Length > 6)
        {
            var lastCodePart = lastTransaction.Substring(6);
            if (long.TryParse(lastCodePart, out long lastCode))
            {
                code = lastCode + 1;
            }
        }

        if (code < 10)
            return $"TC{dateString}0000{code}";
        else if (code < 100)
            return $"TC{dateString}000{code}";
        else if (code < 1000)
            return $"TC{dateString}00{code}";
        else if (code < 10000)
            return $"TC{dateString}0{code}";
        else
            return $"TC{dateString}{code}";
    }

    public async Task<TransactionSummaryResponse> GetSummaryAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _repository.Query()
            .Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        var totalIncome = transactions.Where(x => x.TransactionFlow == TransactionFlows.IN).Sum(x => x.NetAmount);
        var totalExpense = transactions.Where(x => x.TransactionFlow == TransactionFlows.OUT).Sum(x => x.NetAmount);
        var netCashFlow = totalIncome - totalExpense;

        var incomeByType = transactions
            .Where(x => x.TransactionFlow == TransactionFlows.IN)
            .GroupBy(x => x.TransactionType)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.NetAmount));

        var expenseByCategory = transactions
            .Where(x => x.TransactionFlow == TransactionFlows.OUT && !string.IsNullOrEmpty(x.Category))
            .GroupBy(x => x.Category!)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.NetAmount));

        return new TransactionSummaryResponse(
            totalIncome,
            totalExpense,
            netCashFlow,
            transactions.Count,
            incomeByType,
            expenseByCategory
        );
    }

    public async Task<IEnumerable<CashFlowResponse>> GetCashFlowAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _repository.Query()
            .Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        var cashFlow = transactions
            .GroupBy(x => x.TransactionDate.Date)
            .Select(g => new CashFlowResponse(
                g.Key,
                g.Where(x => x.TransactionFlow == TransactionFlows.IN).Sum(x => x.NetAmount),
                g.Where(x => x.TransactionFlow == TransactionFlows.OUT).Sum(x => x.NetAmount),
                g.Where(x => x.TransactionFlow == TransactionFlows.IN).Sum(x => x.NetAmount) -
                g.Where(x => x.TransactionFlow == TransactionFlows.OUT).Sum(x => x.NetAmount)
            ))
            .OrderBy(x => x.Date)
            .ToList();

        return cashFlow;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;

        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }
}
