namespace Application.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IRepository<SupplierPayment, long> _repository;
    private readonly ISupplierPaymentRepository _supplierPaymentRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public SupplierPaymentService(
        IRepository<SupplierPayment, long> repository,
        DefaultValueInjector defaultValueInjector,
        ITenantProvider tenantProvider,
        IUserContextService userContextService,
        IRepository<Company, int> companyRepository,
        ISupplierPaymentRepository supplierPaymentRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _supplierPaymentRepository = supplierPaymentRepository;
    }

    public async Task<SupplierPaymentResponse> AddAsync(SupplierPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            SupplierPaymentValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            SupplierPaymentValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        var entity = request.Adapt<SupplierPayment>();
        entity.BranchId = _currentUser.BranchId;
        // Don't inject audit info here - let the repository handle it based on whether it's new or existing

        var result = await _supplierPaymentRepository.ManageUpdate(request, entity, cancellationToken);
        return result;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _supplierPaymentRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Supplier payment not found !");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Supplier payment not found !");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Supplier payment not found !");

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Supplier payment not found !");

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<SupplierPaymentResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.Supplier)
            .Include(x => x.Customer)
            .Include(x => x.Bank)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<SupplierPaymentResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<SupplierPayment, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<long>(x.Id, x.PaymentNumber)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<SupplierPaymentListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.Supplier)
            .Include(x => x.Customer)
            .Include(x => x.Bank)
            .Include(x => x.Branch)
            .ToListAsync(cancellationToken);
        var response = result.Adapt<IEnumerable<SupplierPaymentListResponse>>();
        return response;
    }

    public async Task<PaginationResult<SupplierPaymentListResponse>> PaginationListAsync(SupplierPaymentPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        // Map frontend column names to entity property names
        if (!string.IsNullOrEmpty(requestQuery.OrderBy))
        {
            var mappedOrderBy = requestQuery.OrderBy switch
            {
                "paymentNumber" => nameof(SupplierPayment.PaymentNumber),
                "paymentDate" => nameof(SupplierPayment.PaymentDate),
                "paymentType" => nameof(SupplierPayment.PaymentType),
                "paymentMethod" => nameof(SupplierPayment.PaymentMethod),
                "paymentAmount" => nameof(SupplierPayment.PaymentAmount),
                "checkNumber" => nameof(SupplierPayment.CheckNumber),
                "checkDate" => nameof(SupplierPayment.CheckDate),
                _ => requestQuery.OrderBy // Keep original if no mapping found
            };
            requestQuery = requestQuery with { OrderBy = mappedOrderBy };
        }

        var status = requestQuery.Status?.ToLowerInvariant() ?? "active";
        Expression<Func<SupplierPayment, bool>> predicate = x => true;

        predicate = status switch
        {
            "archived" => predicate.And(x => !x.IsDeleted && x.IsArchived),
            "deleted" => predicate.And(x => x.IsDeleted && x.TenantId == _tenantId),
            _ => predicate.And(x => !x.IsDeleted && !x.IsArchived)
        };

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchText = requestQuery.OpenText.ToLower();
            predicate = predicate.And(obj => obj.PaymentNumber.ToLower().Contains(searchText)
                            || obj.PaymentAmount.ToString().Contains(searchText)
                            || (obj.Supplier != null && obj.Supplier.SupplierName.ToLower().Contains(searchText))
                            || (obj.Customer != null && obj.Customer.CustomerName.ToLower().Contains(searchText)));
        }

        Expression<Func<SupplierPayment, SupplierPaymentListResponse>>? selector = x => new SupplierPaymentListResponse(
            x.Id,
            x.PaymentNumber,
            x.PaymentDate,
            x.PaymentType,
            x.SupplierId,
            x.Supplier,
            x.CustomerId,
            x.Customer,
            x.PaymentMethod,
            x.BankId,
            x.Bank,
            x.CheckNumber,
            x.CheckDate,
            x.PaymentAmount,
            x.Notes,
            x.BranchId,
            x.Branch,
            x.IsDeleted,
            x.IsArchived,
            x.DeletedAt,
            x.ArchivedAt
        );

        var baseQuery = status == "deleted"
            ? _repository.UnfilteredQuery()
            : _supplierPaymentRepository.Query();

        var query = baseQuery.Where(predicate);

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<SupplierPaymentResponse> UpdateAsync(long id, SupplierPaymentRequest request, CancellationToken cancellationToken = default)
    {
        SupplierPaymentValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _supplierPaymentRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Payment not found !");

        var result = await _supplierPaymentRepository.ManageUpdate(request, existingData, cancellationToken);
        return result;
    }

    public async Task<string> GeneratePaymentNumber(CancellationToken cancellationToken = default)
    {
        return await _supplierPaymentRepository.GeneratePaymentNumber(cancellationToken);
    }

    public async Task<decimal> GetSupplierDueBalance(int supplierId, CancellationToken cancellationToken = default)
    {
        return await _supplierPaymentRepository.GetSupplierDueBalance(supplierId, cancellationToken);
    }

    public async Task<IEnumerable<PurchaseListResponse>> GetPendingPurchases(int supplierId, CancellationToken cancellationToken = default)
    {
        var purchases = await _supplierPaymentRepository.GetPendingPurchases(supplierId, cancellationToken);
        return purchases.Adapt<IEnumerable<PurchaseListResponse>>();
    }

    public async Task<IEnumerable<SalesListResponse>> GetPendingSales(int customerId, CancellationToken cancellationToken = default)
    {
        var sales = await _supplierPaymentRepository.GetPendingSales(customerId, cancellationToken);
        return sales.Adapt<IEnumerable<SalesListResponse>>();
    }
}
