namespace Application.Services;

public class SalesService : ISalesService
{
    private readonly IRepository<Sales, long> _repository;
    private readonly ISalesRepository _salesRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;
    public SalesService(IRepository<Sales, long> repository, ISalesRepository salesRepository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository, IStockRepository stockRepository)
    {
        _repository = repository;
        _salesRepository = salesRepository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _stockRepository = stockRepository;
    }

    public async Task<SalesResponse> AddAsync(SalesRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            SalesValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            SalesValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }


        var entity = request.Adapt<Sales>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Sales, long>(entity);
        _defaultValueInjector.InjectCreatingAudit<SalesDetail, long>(entity.SalesDetails as List<SalesDetail>);

        var result = await _stockRepository.ManageAddSalesStock(entity, cancellationToken);

        var response = result ? entity.Adapt<SalesResponse>() : null;
        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {

        return await _salesRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<SalesResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Include(x => x.SalesDetails).ThenInclude(x => x.Product).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<SalesResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<Sales, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<long>(x.Id, x.InvoiceNumber)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<SalesResponse> UpdateAsync(long id, SalesRequest request, CancellationToken cancellationToken = default)
    {
        SalesValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);


        var existingData = await _salesRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Invoice not found !");

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


        _defaultValueInjector.InjectUpdatingAudit<Sales, long>(existingData);

        var response = await _salesRepository.ManageUpdate(request, existingData, cancellationToken);

        return response;
    }

    public async Task<IEnumerable<SalesListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new SalesListResponse(
                x.Id,
                x.InvoiceNumber,
                x.InvoiceDate,
                x.SalesType,
                x.CustomerId,
                x.Customer,
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
                x.SalesDetails.Select(d => new SalesDetailListResponse(d.Id, d.SalesId, d.ProductId, d.Product.ProductName, d.SalesUnitId, "", d.SalesRate, d.SalesQuantity, d.SalesAmount))
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<SalesListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<Sales, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.InvoiceNumber.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.InvoiceAmount.ToString().Contains(requestQuery.OpenText.ToLower())
                            || obj.Customer.CustomerName.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<Sales, SalesListResponse>>? selector = x => new SalesListResponse(
            x.Id,
            x.InvoiceNumber,
            x.InvoiceDate,
            x.SalesType,
            x.CustomerId,
            x.Customer,
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
            x.SalesDetails.Select(d => new SalesDetailListResponse(d.Id, d.SalesId, d.ProductId, d.Product.ProductName, d.SalesUnitId, "", d.SalesRate, d.SalesQuantity, d.SalesAmount))
            );

        var query = _salesRepository.Query();
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
            var code = long.Parse((await _repository.Query().Where(x => x.BranchId == _currentUser.BranchId && x.InvoiceDate.Month == currentDate.Month).OrderByDescending(x => x.InvoiceNumber).Select(x => x.InvoiceNumber).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 4) ?? "0") + 1;
            var range = code / 10;

            if (range == 0)
                return $"{dateString}0000{code}";//P-00099
            else if (range <= 9)
                return $"{dateString}000{code}";//P-00099
            else if (range <= 99)
                return $"{dateString}00{code}"; //P-00999
            else if (range <= 999)
                return $"{dateString}0{code}"; //P-09999
            else
                return $"{dateString}{code}"; //P-99999
        }
        else
        {
            var code = long.Parse((await _repository.Query().OrderByDescending(x => x.InvoiceNumber).Select(x => x.InvoiceNumber).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 4) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"{dateString}0000{code}";//P-00099
            else if (range <= 9)
                return $"{dateString}000{code}";//P-00099
            else if (range <= 99)
                return $"{dateString}00{code}"; //P-00999
            else if (range <= 999)
                return $"{dateString}0{code}"; //P-09999
            else
                return $"{dateString}{code}"; //P-99999
        }
    }

}
