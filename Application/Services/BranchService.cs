namespace Application.Services;

public class BranchService : IBranchService
{
    private readonly IRepository<Branch, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly IUserContextService _userContextService;
    private readonly CurrentUser _currentUser;
    public BranchService(IRepository<Branch, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _userContextService = userContextService;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser(); // Keep this for now since it's used in many places
        _companyRepository = companyRepository;
    }

    public async Task<BranchResponse> AddAsync(BranchRequest branch, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            BranchValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(branch, cancellationToken);
        }
        else
        {
            BranchValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(branch, cancellationToken);
        }


        var entity = branch.Adapt<Branch>();
        _defaultValueInjector.InjectTenant<Branch, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<BranchResponse>() : null;
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
        var result = await _repository.DeletableQuery(x => ids.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        return result > 0;
    }

    public async Task<BranchResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<BranchResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Branch, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.Name)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<BranchResponse> UpdateAsync(int id, BranchRequest branch, CancellationToken cancellationToken = default)
    {
        BranchValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(branch, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) return null;

        var existingBranchCode = existingData.BranchCode;
        var entity = branch.Adapt(existingData);
        entity.BranchCode = existingBranchCode;
        _defaultValueInjector.InjectTenant<Branch, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) return null;


        var response = entity.Adapt<BranchResponse>();
        return response;
    }

    public async Task<BranchResponse> ExecuteUpdateAsync(int id, BranchRequest branch, CancellationToken cancellationToken = default)
    {
        BranchValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(branch, cancellationToken);

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.Name, branch.Name)
               .SetProperty(cmd => cmd.BranchCode, branch.BranchCode)
               .SetProperty(cmd => cmd.BusinessCurrency, branch.BusinessCurrency)
               .SetProperty(cmd => cmd.CurrencySymbol, branch.CurrencySymbol)
               .SetProperty(cmd => cmd.IsActive, branch.IsActive)
               .SetProperty(cmd => cmd.Phone, branch.Phone)
               .SetProperty(cmd => cmd.Address, branch.Address)
               .SetProperty(cmd => cmd.Name, branch.Name)
               .SetProperty(cmd => cmd.Address, branch.Address)
               .SetProperty(cmd => cmd.AutoInvoicePrint, branch.AutoInvoicePrint)
               .SetProperty(cmd => cmd.InvoiceHeader, branch.InvoiceHeader)
               .SetProperty(cmd => cmd.InvoiceFooter, branch.InvoiceFooter)
               .SetProperty(cmd => cmd.IsMainBranch, branch.IsMainBranch)
               .SetProperty(cmd => cmd.IsActive, branch.IsActive)
        );

        var response = branch.Adapt<BranchResponse>();
        return response;
    }

    public async Task<IEnumerable<BranchListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new BranchListResponse(
               x.Id,
               x.Name,
               x.BranchCode,
               x.BusinessCurrency,
               x.CurrencySymbol,
               x.Phone,
               x.Address,
               x.AutoInvoicePrint,
               x.InvoiceHeader,
               x.InvoiceFooter,
               x.IsMainBranch,
               x.Status
               ))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<BranchListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<Branch, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.Name.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.BranchCode.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Phone.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<Branch, BranchListResponse>>? selector = x => new BranchListResponse(
               x.Id,
               x.Name,
               x.BranchCode,
               x.BusinessCurrency,
               x.CurrencySymbol,
               x.Phone,
               x.Address,
               x.AutoInvoicePrint,
               x.InvoiceHeader,
               x.InvoiceFooter,
               x.IsMainBranch,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (dependOn == ECodeGeneration.Branch)
        {
            var code = int.Parse((await _repository.Query().OrderByDescending(x => x.BranchCode).Select(x => x.BranchCode).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 2) ?? "0") + 1;
            var range = code / 10;

            if (range == 0)
                return $"B-0000{code}";//B-00099
            else if (range <= 9)
                return $"B-000{code}";//B-00099
            else if (range <= 99)
                return $"B-00{code}"; //B-00999
            else if (range <= 999)
                return $"B-0{code}"; //B-09999
            else
                return $"B-{code}"; //B-99999
        }
        else
        {
            var code = int.Parse((await _repository.Query().OrderByDescending(x => x.BranchCode).Select(x => x.BranchCode).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 2) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"B-0000{code}";//B-00099
            else if (range <= 9)
                return $"B-000{code}";//B-00099
            else if (range <= 99)
                return $"B-00{code}"; //B-00999
            else if (range <= 999)
                return $"B-0{code}"; //B-09999
            else
                return $"B-{code}"; //B-99999
        }
    }

}
