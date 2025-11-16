namespace Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IRepository<Supplier, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;
    public SupplierService(IRepository<Supplier, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<SupplierResponse> AddAsync(SupplierRequest customer, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            SupplierValidator validator = new(_repository, branchId: _currentUser.BranchId);
            await validator.ValidateAndThrowAsync(customer, cancellationToken);
        }
        else
        {
            SupplierValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(customer, cancellationToken);
        }


        var entity = customer.Adapt<Supplier>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Supplier, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<SupplierResponse>() : null;
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

    public async Task<SupplierResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);
        var response = result is not null ? result.Adapt<SupplierResponse>() : null;
        return response;
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Supplier, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _repository.Query().Where(predicate).Select(x => new Lookup<int>(x.Id, x.SupplierName)).ToListAsync();
        return result;
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
        => await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);

    public async Task<SupplierResponse> UpdateAsync(int id, SupplierRequest customer, CancellationToken cancellationToken = default)
    {
        SupplierValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(customer, cancellationToken);

        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        var entity = customer.Adapt(existingData);

        _defaultValueInjector.InjectUpdatingAudit<Supplier, int>(entity);
        var result = await _repository.UpdateAsync(entity, cancellationToken);
        if (result is null) return null;


        var response = entity.Adapt<SupplierResponse>();
        return response;
    }

    public async Task<SupplierResponse> ExecuteUpdateAsync(int id, SupplierRequest customer, CancellationToken cancellationToken = default)
    {
        SupplierValidator validator = new(_repository, id);
        await validator.ValidateAndThrowAsync(customer, cancellationToken);

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.SupplierName, customer.SupplierName)
               .SetProperty(cmd => cmd.Address, customer.Address)
               .SetProperty(cmd => cmd.BranchId, customer.BranchId)
               .SetProperty(cmd => cmd.CreditLimit, customer.CreditLimit)
               .SetProperty(cmd => cmd.SupplierEmail, customer.SupplierEmail)
               .SetProperty(cmd => cmd.SupplierMobile, customer.SupplierMobile)
               .SetProperty(cmd => cmd.ImageUrl, customer.ImageUrl)
               .SetProperty(cmd => cmd.OpeningBalance, customer.OpeningBalance)
               .SetProperty(cmd => cmd.OfficePhone, customer.OfficePhone)
               .SetProperty(cmd => cmd.IsActive, customer.IsActive)
        );

        var response = customer.Adapt<SupplierResponse>();
        return response;
    }

    public async Task<IEnumerable<SupplierListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _repository.Query()
           .Select(x => new SupplierListResponse(
               x.Id,
               x.SupplierName,
               x.SupplierCode,
               x.SupplierBarcode,
               x.SupplierMobile,
               x.SupplierEmail,
               x.OfficePhone,
               x.Address,
               x.ImageUrl,
               x.CreditLimit,
               x.OpeningBalance,
               x.PreviousDue,
               x.IsSystemDefault,
               x.Status))
           .ToListAsync(cancellationToken);
        return response;
    }

    public async Task<PaginationResult<SupplierListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {

        Expression<Func<Supplier, bool>>? predicate = x => x.IsSystemDefault == false;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.SupplierName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.SupplierMobile.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.SupplierEmail.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.OfficePhone.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.Address.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.SupplierCode.ToLower().Contains(requestQuery.OpenText.ToLower());
        }

        Expression<Func<Supplier, SupplierListResponse>>? selector = x => new SupplierListResponse(
               x.Id,
               x.SupplierName,
               x.SupplierCode,
               x.SupplierBarcode,
               x.SupplierMobile,
               x.SupplierEmail,
               x.OfficePhone,
               x.Address,
               x.ImageUrl,
               x.CreditLimit,
               x.OpeningBalance,
               x.PreviousDue,
               x.IsSystemDefault,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var dependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (dependOn == ECodeGeneration.Branch)
        {
            var code = int.Parse((await _repository.Query().Where(x => x.IsSystemDefault == false && x.BranchId == _currentUser.BranchId).OrderByDescending(x => x.SupplierCode).Select(x => x.SupplierCode).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 2) ?? "0") + 1;
            var range = code / 10;

            if (range == 0)
                return $"S-0000{code}";//P-00099
            else if (range <= 9)
                return $"S-000{code}";//P-00099
            else if (range <= 99)
                return $"S-00{code}"; //P-00999
            else if (range <= 999)
                return $"S-0{code}"; //P-09999
            else
                return $"S-{code}"; //P-99999
        }
        else
        {
            var code = int.Parse((await _repository.Query().Where(x => x.IsSystemDefault == false).OrderByDescending(x => x.SupplierCode).Select(x => x.SupplierCode).FirstOrDefaultAsync(cancellationToken))?.Remove(0, 2) ?? "0") + 1;

            var range = code / 10;

            if (range == 0)
                return $"S-0000{code}";//P-00099
            else if (range <= 9)
                return $"S-000{code}";//P-00099
            else if (range <= 99)
                return $"S-00{code}"; //P-00999
            else if (range <= 999)
                return $"S-0{code}"; //P-09999
            else
                return $"S-{code}"; //P-99999
        }
    }

}
