namespace Application.Services;

public class SaleReturnService : ISaleReturnService
{
    private readonly IRepository<SaleReturn, long> _repository;
    private readonly ISaleReturnRepository _saleReturnRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public SaleReturnService(IRepository<SaleReturn, long> repository, ISaleReturnRepository saleReturnRepository, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository, IStockRepository stockRepository)
    {
        _repository = repository;
        _saleReturnRepository = saleReturnRepository;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
        _stockRepository = stockRepository;
    }

    public async Task<SaleReturnResponse> AddAsync(SaleReturnRequest request, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            SaleReturnValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }
        else
        {
            SaleReturnValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        var entity = request.Adapt<SaleReturn>();
        entity.BranchId = _currentUser.BranchId;
        entity.ReturnDate = DateTime.UtcNow;
        // Audit fields will be automatically set by DbContext.SaveChangesAsync

        var result = await _stockRepository.ManageAddSaleReturnStock(entity, cancellationToken);

        var response = result ? entity.Adapt<SaleReturnResponse>() : throw new Exception("Failed to create sale return");
        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.Query().Include(x => x.SaleReturnDetails).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existingData == null) return false;

        var result = await _stockRepository.ManageDeleteSaleReturnStock(existingData, cancellationToken);
        return result;
    }

    public async Task<bool> DeleteBatchAsync(List<long> ids, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.Query().Include(x => x.SaleReturnDetails).Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (!existingData.Any()) return false;

        var result = await _stockRepository.ManageBatchDeleteSaleReturnStock(existingData, cancellationToken);
        return result;
    }

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Sale return not found !");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = _currentUser.Id;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.UnfilteredQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Sale return not found !");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedById = null;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Sale return not found !");

        entity.IsArchived = true;
        entity.ArchivedAt = DateTime.UtcNow;
        entity.ArchivedById = _currentUser.Id;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) throw new Exception("Sale return not found !");

        entity.IsArchived = false;
        entity.ArchivedAt = null;
        entity.ArchivedById = null;
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }

    public async Task<SaleReturnResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query()
            .Include(x => x.SaleReturnDetails)
            .ThenInclude(x => x.Product)
            .Include(x => x.Sales)
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        var response = result is not null ? result.Adapt<SaleReturnResponse>() : throw new Exception("Sale Return not found");
        return response;
    }

    public async Task<IEnumerable<Lookup<long>>> GetLookup(Expression<Func<SaleReturn, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<long>(x.Id, x.ReturnNumber)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(long id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<SaleReturnResponse> UpdateAsync(long id, SaleReturnRequest request, CancellationToken cancellationToken = default)
    {
        SaleReturnValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingData = await _saleReturnRepository.GetByIdAsync(id, cancellationToken);
        if (existingData == null) throw new Exception("Sale Return not found !");

        existingData.DiscountAmount = request.DiscountAmount;
        existingData.DiscountPercent = request.DiscountPercent;
        existingData.ReturnAmount = request.ReturnAmount;
        existingData.OtherCost = request.OtherCost;
        existingData.Subtotal = request.Subtotal;
        existingData.VatAmount = request.VatAmount;
        existingData.VatPercent = request.VatPercent;
        existingData.Reason = request.Reason;

        // Audit fields will be automatically set by DbContext.SaveChangesAsync

        var response = await _saleReturnRepository.ManageUpdate(request, existingData, cancellationToken);

        return response;
    }

    public async Task<IEnumerable<SaleReturnListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Include(x => x.Sales)
           .Include(x => x.Customer)
           .Include(x => x.Branch)
           .Select(x => new SaleReturnListResponse(
                x.Id,
                x.ReturnNumber,
                x.ReturnDate,
                x.SalesId,
                x.Sales.InvoiceNumber,
                x.CustomerId,
                x.Customer,
                x.Subtotal,
                x.VatPercent,
                x.VatAmount,
                x.DiscountPercent,
                x.DiscountAmount,
                x.OtherCost,
                x.ReturnAmount,
                x.Reason,
                x.BranchId,
                x.Branch,
                x.IsDeleted,
                x.IsArchived,
                x.DeletedAt,
                x.ArchivedAt,
                x.SaleReturnDetails.Select(d => new SaleReturnDetailListResponse(d.Id, d.SaleReturnId, d.ProductId, d.Product.ProductName, d.ReturnUnitId, "", d.ReturnRate, (float)d.ReturnQuantity, d.ReturnAmount, d.Reason))
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<SaleReturnListResponse>> PaginationListAsync(SaleReturnPaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        var status = requestQuery.Status?.ToLowerInvariant() ?? "active";
        Expression<Func<SaleReturn, bool>> predicate = x => true;

        predicate = status switch
        {
            "archived" => predicate.And(x => !x.IsDeleted && x.IsArchived),
            "deleted" => predicate.And(x => x.IsDeleted && x.TenantId == _tenantId),
            _ => predicate.And(x => !x.IsDeleted && !x.IsArchived)
        };

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchText = requestQuery.OpenText.ToLower();
            predicate = predicate.And(obj => obj.ReturnNumber.ToLower().Contains(searchText)
                            || obj.ReturnAmount.ToString().Contains(searchText)
                            || obj.Customer.CustomerName.ToLower().Contains(searchText)
                            || obj.Sales.InvoiceNumber.ToLower().Contains(searchText));
        }

        Expression<Func<SaleReturn, SaleReturnListResponse>>? selector = x => new SaleReturnListResponse(
            x.Id,
            x.ReturnNumber,
            x.ReturnDate,
            x.SalesId,
            x.Sales.InvoiceNumber,
            x.CustomerId,
            x.Customer,
            x.Subtotal,
            x.VatPercent,
            x.VatAmount,
            x.DiscountPercent,
            x.DiscountAmount,
            x.OtherCost,
            x.ReturnAmount,
            x.Reason,
            x.BranchId,
            x.Branch,
            x.IsDeleted,
            x.IsArchived,
            x.DeletedAt,
            x.ArchivedAt,
            x.SaleReturnDetails.Select(d => new SaleReturnDetailListResponse(d.Id, d.SaleReturnId, d.ProductId, d.Product.ProductName, d.ReturnUnitId, "", d.ReturnRate, (float)d.ReturnQuantity, d.ReturnAmount, d.Reason))
            );

        var baseQuery = status == "deleted"
            ? _repository.UnfilteredQuery()
            : _saleReturnRepository.Query();

        var query = baseQuery.Where(predicate);
        return await _repository.PaginationQuery(query, paginationQuery: requestQuery, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateReturnNumber(CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow;
        var year = currentDate.Year.ToString()?.Remove(0, 2);
        var month = currentDate.Month / 10 == 0 ? "0" + currentDate.Month : currentDate.Month.ToString();
        var dateString = $"{year}{month}";
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (dependOn == ECodeGeneration.Branch)
        {
            var code = long.Parse((await _repository.Query().Where(x => x.BranchId == _currentUser.BranchId && x.ReturnDate.Month == currentDate.Month).OrderByDescending(x => x.ReturnNumber).Select(x => x.ReturnNumber).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 4) ?? "0") + 1;
            var range = code / 10;
            var totalCode = range == 0 ? $"SRN{dateString}000{code}" : range < 10 ? $"SRN{dateString}00{code}" : range < 100 ? $"SRN{dateString}0{code}" : $"SRN{dateString}{code}";
            return totalCode;
        }
        else
        {
            var code = long.Parse((await _repository.Query().Where(x => x.ReturnDate.Month == currentDate.Month).OrderByDescending(x => x.ReturnNumber).Select(x => x.ReturnNumber).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 4) ?? "0") + 1;
            var range = code / 10;
            var totalCode = range == 0 ? $"SRN{dateString}000{code}" : range < 10 ? $"SRN{dateString}00{code}" : range < 100 ? $"SRN{dateString}0{code}" : $"SRN{dateString}{code}";
            return totalCode;
        }
    }
}
