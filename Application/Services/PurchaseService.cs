namespace Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IRepository<Purchase, long> _repository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;
    public PurchaseService(IRepository<Purchase, long> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository, IStockRepository stockRepository, IPurchaseRepository purchaseRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _stockRepository = stockRepository;
        _purchaseRepository = purchaseRepository;
    }

    public async Task<PurchaseResponse> AddAsync(PurchaseRequest product, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            PurchaseValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(product, cancellationToken);
        }
        else
        {
            PurchaseValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(product, cancellationToken);
        }


        var entity = product.Adapt<Purchase>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Purchase, long>(entity);
        _defaultValueInjector.InjectCreatingAudit<PurchaseDetail, long>(entity.PurchaseDetails as List<PurchaseDetail>);

        var result = await _stockRepository.ManageAddPurchaseStock(entity, cancellationToken);

        var response = result ? entity.Adapt<PurchaseResponse>() : null;
        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _purchaseRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<PurchaseResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Include(x => x.PurchaseDetails).ThenInclude(x => x.Product).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<PurchaseResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<Purchase, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<long>(x.Id, x.InvoiceNumber)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<PurchaseResponse> UpdateAsync(long id, PurchaseRequest request, CancellationToken cancellationToken = default)
    {
        PurchaseValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _purchaseRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Invoice not found !");


        // var prevHistoryDictionary = existingData.PurchaseDetails.Select(x => new PurchaseDictionary(x.ProductId, x.PurchaseQuantity, x.PurchaseRate, x.PurchaseUnitId)).ToList();


        existingData.BranchId = _currentUser.BranchId;
        existingData.DiscountAmount = request.DiscountAmount;
        existingData.DiscountPercent = request.DiscountPercent;
        existingData.InvoiceAmount = request.InvoiceAmount;
        existingData.InvoiceDate = request.InvoiceDate;
        existingData.OtherCost = request.OtherCost;
        existingData.PaidAmount = request.PaidAmount;
        existingData.Subtotal = request.Subtotal;
        existingData.VatAmount = request.VatAmount;
        existingData.VatPercent = request.VatPercent;


        _defaultValueInjector.InjectUpdatingAudit<Purchase, long>(existingData);

        var response = await _purchaseRepository.ManageUpdate(request, existingData, cancellationToken);

        return response;
    }

    public async Task<IEnumerable<PurchaseListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new PurchaseListResponse(
                x.Id,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.SupplierId,
                x.Supplier,
                x.Subtotal,
                x.VatPercent,
                x.VatAmount,
                x.DiscountPercent,
                x.DiscountAmount,
                x.OtherCost,
                x.InvoiceAmount,
                x.PaidAmount,
                x.BranchId,
                x.Branch,
                x.PurchaseDetails.Select(d => new PurchaseDetailListResponse(d.Id, d.PurchaseId, d.ProductId, d.Product.ProductName, d.PurchaseUnitId, "", d.PurchaseRate, d.PurchaseQuantity, d.PurchaseAmount))
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<PurchaseListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<Purchase, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.InvoiceNumber.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.InvoiceAmount.ToString().Contains(requestQuery.OpenText.ToLower())
                            || obj.Supplier.SupplierName.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<Purchase, PurchaseListResponse>>? selector = x => new PurchaseListResponse(
            x.Id,
            x.InvoiceNumber,
            x.InvoiceDate,
            x.SupplierId,
            x.Supplier,
            x.Subtotal,
            x.VatPercent,
            x.VatAmount,
            x.DiscountPercent,
            x.DiscountAmount,
            x.OtherCost,
            x.InvoiceAmount,
            x.PaidAmount,
            x.BranchId,
            x.Branch,
            x.PurchaseDetails.Select(d => new PurchaseDetailListResponse(d.Id, d.PurchaseId, d.ProductId, d.Product.ProductName, d.PurchaseUnitId, "", d.PurchaseRate, d.PurchaseQuantity, d.PurchaseAmount))
            );

        var query = _purchaseRepository.Query();

        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateInvoiceNumber(CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.Now;
        var year = currentDate.Year.ToString()?.Remove(0, 2);
        var month = currentDate.Month / 10 == 0 ? "0" + currentDate.Month : currentDate.Month.ToString();
        var dateString = $"{year}{month}";
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (dependOn == ECodeGeneration.Branch)
        {
            var code = long.Parse((await _repository.Query().Where(x => x.BranchId == _currentUser.BranchId && x.InvoiceDate.Month == currentDate.Month).OrderByDescending(x => x.InvoiceNumber).Select(x => x.InvoiceNumber).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 5) ?? "0") + 1;
            var range = code / 10;

            if (range == 0)
                return $"P{dateString}0000{code}";//P-00099
            else if (range <= 9)
                return $"P{dateString}000{code}";//P-00099
            else if (range <= 99)
                return $"P{dateString}00{code}"; //P-00999
            else if (range <= 999)
                return $"P{dateString}0{code}"; //P-09999
            else
                return $"P{dateString}{code}"; //P-99999
        }
        else
        {
            var code = long.Parse((await _repository.Query().OrderByDescending(x => x.InvoiceNumber).Select(x => x.InvoiceNumber).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 5) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"P{dateString}0000{code}";//P-00099
            else if (range <= 9)
                return $"P{dateString}000{code}";//P-00099
            else if (range <= 99)
                return $"P{dateString}00{code}"; //P-00999
            else if (range <= 999)
                return $"P{dateString}0{code}"; //P-09999
            else
                return $"P{dateString}{code}"; //P-99999
        }
    }

}

