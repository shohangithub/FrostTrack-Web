namespace Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IRepository<Transaction, Guid> _repository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public TransactionService(
        IRepository<Transaction, Guid> repository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        ICodeGenerationService codeGenerationService)
    {
        _repository = repository;
        _transactionHeadRepository = transactionHeadRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _codeGenerationService = codeGenerationService;
    }

    public async Task<TransactionResponse> AddAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        TransactionValidator validator = new(_repository, _transactionHeadRepository);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        // Load TransactionHead to get Type and DisplayType
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == request.TransactionHeadId, cancellationToken);

        if (transactionHead == null)
            throw new Exception("Transaction head not found!");

        var entity = request.Adapt<Transaction>();
        entity.BranchId = _currentUser.BranchId;

        // Set default PaymentMethod to CASH if not provided
        if (string.IsNullOrEmpty(entity.PaymentMethod))
        {
            entity.PaymentMethod = PaymentMethods.CASH;
        }

        // Set default Description if empty
        if (string.IsNullOrEmpty(entity.Description))
        {
            entity.Description = $"{transactionHead.Name} - {transactionHead.DisplayType}";
        }

        // Store amounts as positive values; direction is determined by TransactionHead.Type (DEBIT = out, CREDIT = in)
        entity.Amount = Math.Abs(entity.Amount);

        // Calculate NetAmount: Amount - Discount + Adjustment (always positive)
        entity.NetAmount = entity.Amount - entity.DiscountAmount + entity.AdjustmentValue;

        entity.TransactionDate = DateTime.UtcNow;
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
            .Include(x => x.Employee)
            .Include(x => x.TransactionHead)
            .Include(x => x.SalaryPayment)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (result != null && result.TransactionHead != null)
            result.TransactionHead.Type = string.IsNullOrEmpty(result.TransactionHead.DisplayType) ? result.TransactionHead.Type : result.TransactionHead.DisplayType;

        if (result is null) return null;

        var response = result.Adapt<TransactionResponse>();
        // Manually map EmployeeName from Employee object
        response = response with { EmployeeName = result.Employee?.EmployeeName };

        // For salary payments, populate bonus/deduction from the SalaryPayment record
        if (result.TransactionHead?.UsageFor == UsageFor.SALARY && result.SalaryPayment != null)
        {
            response = response with
            {
                Bonus = result.SalaryPayment.Bonus,
                Deduction = result.SalaryPayment.Deduction
            };
        }

        // Fetch related labour charge if this is a BILL_COLLECTION linked to a delivery
        if (result.TransactionHead?.UsageFor == UsageFor.BILL_COLLECTION && result.DeliveryId.HasValue)
        {
            var labourCharge = await _repository.Query()
                .Where(t => t.TransactionCode == result.TransactionCode + "-L" &&
                           t.TransactionHead!.UsageFor == UsageFor.LABOUR_CHARGE &&
                           t.DeliveryId == result.DeliveryId)
                .Select(t => (decimal?)t.NetAmount)
                .FirstOrDefaultAsync(cancellationToken);

            response = response with { RelatedLabourCharge = labourCharge };
        }

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

    public async Task<IEnumerable<Lookup<Guid>>> GetLookupByUsageFor(string usageFor, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.TransactionHead)
            .Where(x => x.TransactionHead!.UsageFor == usageFor)
            .Select(x => new Lookup<Guid>(x.Id, x.TransactionCode))
            .ToListAsync(cancellationToken);
        return result;
    }

    public async Task<TransactionResponse?> GetByTransactionCodeAsync(string transactionCode, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .Include(x => x.Employee)
            .Include(x => x.TransactionHead)
            .FirstOrDefaultAsync(x => x.TransactionCode == transactionCode, cancellationToken);

        if (result != null && result.TransactionHead != null)
            result.TransactionHead.Type = string.IsNullOrEmpty(result.TransactionHead.DisplayType) ? result.TransactionHead.Type : result.TransactionHead.DisplayType;

        if (result is null) return null;

        var response = result.Adapt<TransactionResponse>();
        // Manually map EmployeeName from Employee object
        response = response with { EmployeeName = result.Employee?.EmployeeName };
        return response;
    }

    public async Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<TransactionResponse> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken = default)
    {
        TransactionValidator validator = new(_repository, _transactionHeadRepository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Transaction not found!");

        // Load TransactionHead to get Type and DisplayType
        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == request.TransactionHeadId, cancellationToken);

        if (transactionHead == null)
            throw new Exception("Transaction head not found!");

        request.Adapt(entity);
        entity.BranchId = _currentUser.BranchId;

        // Set default PaymentMethod to CASH if not provided
        if (string.IsNullOrEmpty(entity.PaymentMethod))
        {
            entity.PaymentMethod = PaymentMethods.CASH;
        }

        // Store amounts as positive values; direction is determined by TransactionHead.Type (DEBIT = out, CREDIT = in)
        entity.Amount = Math.Abs(entity.Amount);

        // Calculate NetAmount: Amount - Discount + Adjustment (always positive)
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
            .Include(x => x.Employee)
            .Include(x => x.TransactionHead)
            .Select(x => new TransactionListResponse(
                x.Id,
                x.TransactionCode,
                x.TransactionDate,
                x.TransactionHeadId,
                new TransactionHeadLookup
                (
                    x.TransactionHead!.Id,
                    x.TransactionHead!.Name,
                    !string.IsNullOrWhiteSpace(x.TransactionHead!.DisplayType) ? x.TransactionHead!.DisplayType : x.TransactionHead!.Type
                ),
                x.BranchId,
                x.Branch!.Name,
                x.CustomerId,
                x.Customer != null ? x.Customer.CustomerName : null,
                x.EmployeeId,
                x.Employee != null ? x.Employee.EmployeeName : null,
                x.NetAmount,
                x.PaymentMethod,
                x.Description,
                x.IsDeleted,
                x.IsArchived,
                x.DeletedAt,
                x.ArchivedAt,
                null
            ))
            .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<TransactionListResponse>> PaginationListAsync(TransactionPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        // Map frontend column names to entity property names
        if (!string.IsNullOrEmpty(requestQuery.OrderBy))
        {
            var mappedOrderBy = requestQuery.OrderBy switch
            {
                "transactionCode" => nameof(Transaction.TransactionCode),
                "transactionDate" => nameof(Transaction.TransactionDate),
                "netAmount" => nameof(Transaction.NetAmount),
                "amount" => nameof(Transaction.Amount),
                "paymentMethod" => nameof(Transaction.PaymentMethod),
                "description" => nameof(Transaction.Description),
                "customerName" => nameof(Transaction.CustomerId), // Sort by CustomerId instead of navigation property
                "vendorName" => nameof(Transaction.Description),
                _ => requestQuery.OrderBy
            };
            requestQuery = requestQuery with { OrderBy = mappedOrderBy };
        }

        var status = requestQuery.Status?.ToLowerInvariant() ?? "active";

        Expression<Func<Transaction, bool>> predicate = x => true;

        // Status-based filter — drives which set of records is shown
        predicate = status switch
        {
            "archived" => predicate.And(x =>
                !x.IsDeleted && x.IsArchived &&
                x.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                x.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE),
            "deleted" => predicate.And(x =>
                x.IsDeleted && x.TenantId == _tenantId),
            _ => predicate.And(x =>
                !x.IsDeleted && !x.IsArchived &&
                x.TransactionHead!.UsageFor != UsageFor.OPENING_BALANCE &&
                x.TransactionHead!.UsageFor != UsageFor.CLOSING_BALANCE)
        };

        if (requestQuery.UsageFor != null && status != "deleted")
        {
            predicate = predicate.And(x =>
                x.TransactionHead != null &&
                x.TransactionHead.UsageFor == requestQuery.UsageFor);
        }

        if (!string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchText = requestQuery.OpenText.Trim().ToLower();

            predicate = predicate.And(obj =>
                (obj.TransactionCode != null && obj.TransactionCode.ToLower().Contains(searchText)) ||
                (obj.Description != null && obj.Description.ToLower().Contains(searchText)) ||
                obj.NetAmount.ToString().Contains(searchText) ||
                obj.Amount.ToString().Contains(searchText) ||
                (obj.Customer != null &&
                obj.Customer.CustomerName != null &&
                obj.Customer.CustomerName.ToLower().Contains(searchText))
            );
        }

        Expression<Func<Transaction, TransactionListResponse>>? selector = x => new TransactionListResponse(
            x.Id,
            x.TransactionCode,
            x.TransactionDate,
            x.TransactionHeadId,
             new TransactionHeadLookup
                (
                    x.TransactionHead!.Id,
                    x.TransactionHead!.Name,
                    !string.IsNullOrWhiteSpace(x.TransactionHead!.DisplayType) ? x.TransactionHead!.DisplayType : x.TransactionHead!.Type
                ),
            x.BranchId,
            x.Branch!.Name,
            x.CustomerId,
            x.Customer != null ? x.Customer.CustomerName : null,
            x.EmployeeId,
            x.Employee != null ? x.Employee.EmployeeName : null,
            x.NetAmount,
            x.PaymentMethod,
            x.Description,
            x.IsDeleted,
            x.IsArchived,
            x.DeletedAt,
            x.ArchivedAt,
            // Find related labour charge transaction for this BILL_COLLECTION
            x.TransactionHead!.UsageFor == UsageFor.BILL_COLLECTION && x.DeliveryId != null
                ? _repository.Query()
                    .Where(t => t.TransactionCode == x.TransactionCode + "-L" &&
                               t.TransactionHead!.UsageFor == UsageFor.LABOUR_CHARGE &&
                               t.DeliveryId == x.DeliveryId)
                    .Select(t => (decimal?)t.NetAmount)
                    .FirstOrDefault()
                : null
        );

        // For the "deleted" view, bypass the global IsDeleted query filter
        var baseQuery = status == "deleted"
            ? _repository.UnfilteredQuery()
            : _repository.Query();

        var query = baseQuery
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.TransactionHead)
            .AsQueryable();

        // Apply search predicate if it exists
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateTransactionCode(CancellationToken cancellationToken = default)
    {
        return await _codeGenerationService.GenerateCodeAsync(
            _repository.Query(),
            "TR",
            t => t.TransactionCode,
            cancellationToken);
    }

    public async Task<TransactionSummaryResponse> GetSummaryAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default)
    {
        var query = _repository.Query().Include(x => x.TransactionHead)
            .Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        var totalIncome = transactions.Where(x => x.TransactionHead!.Type == TransactionHeadTypes.CREDIT).Sum(x => x.NetAmount);
        var totalExpense = transactions.Where(x => x.TransactionHead!.Type == TransactionHeadTypes.DEBIT).Sum(x => x.NetAmount);
        var netCashFlow = totalIncome - totalExpense;

        var incomeByType = transactions
            .Where(x => x.TransactionHead!.Type == TransactionHeadTypes.CREDIT)
            .GroupBy(x => x.TransactionHead!.Name)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.NetAmount));

        var expenseByCategory = transactions
            .Where(x => x.TransactionHead!.Type == TransactionHeadTypes.DEBIT)
            .GroupBy(x => x.TransactionHead!.Name)
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
        var query = _repository.Query().Include(x => x.TransactionHead)
            .Where(x => x.TransactionDate >= startDate && x.TransactionDate <= endDate);

        if (branchId.HasValue)
            query = query.Where(x => x.BranchId == branchId.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        var cashFlow = transactions
            .GroupBy(x => x.TransactionDate.Date)
            .Select(g => new CashFlowResponse(
                g.Key,
                g.Where(x => x.TransactionHead!.Type == TransactionHeadTypes.CREDIT).Sum(x => x.NetAmount),
                g.Where(x => x.TransactionHead!.Type == TransactionHeadTypes.DEBIT).Sum(x => x.NetAmount),
                g.Sum(x => x.NetAmount)
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
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
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
