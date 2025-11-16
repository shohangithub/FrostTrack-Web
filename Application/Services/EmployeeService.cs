namespace Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee, int> _repository;
    private readonly IRepository<Company, int> _companyRepository;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly ITenantProvider _tenantProvider;
    private readonly Guid _tenantId;
    private readonly CurrentUser _currentUser;

    public EmployeeService(IRepository<Employee, int> repository, DefaultValueInjector defaultValueInjector, ITenantProvider tenantProvider, IUserContextService userContextService, IRepository<Company, int> companyRepository)
    {
        _repository = repository;
        _defaultValueInjector = defaultValueInjector;
        _tenantProvider = tenantProvider;
        _tenantId = _tenantProvider.GetTenantId();
        _currentUser = userContextService.GetCurrentUser();
        _companyRepository = companyRepository;
    }

    public async Task<EmployeeResponse> AddAsync(EmployeeRequest employee, CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            EmployeeValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(employee, cancellationToken);
        }
        else
        {
            EmployeeValidator validator = new(_repository);
            await validator.ValidateAndThrowAsync(employee, cancellationToken);
        }

        var entity = employee.Adapt<Employee>();
        entity.BranchId = _currentUser.BranchId;
        _defaultValueInjector.InjectCreatingAudit<Employee, int>(entity);
        var result = await _repository.AddAsync(entity, cancellationToken);
        var response = result ? entity.Adapt<EmployeeResponse>() : throw new InvalidOperationException("Failed to create employee");
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
        var existingData = await _repository.Query().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        if (existingData is null || !existingData.Any()) throw new ArgumentNullException(nameof(existingData));

        foreach (var entity in existingData)
        {
            await _repository.DeleteAsync(entity, cancellationToken);
        }
        return true;
    }

    public async Task<EmployeeResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        return existingData?.Adapt<EmployeeResponse>() ?? throw new ArgumentNullException(nameof(existingData));
    }

    public async Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Employee, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _repository.Query()
            .Where(predicate)
            .Select(x => new Lookup<int> { Value = x.Id, Text = x.EmployeeName })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.Query().AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<EmployeeListResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var data = await _repository.Query().ToListAsync(cancellationToken);
        return data.Adapt<IEnumerable<EmployeeListResponse>>();
    }

    public async Task<PaginationResult<EmployeeListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default)
    {
        Expression<Func<Employee, bool>>? predicate = x => true;

        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            predicate = obj => obj.EmployeeName.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || obj.EmployeeCode.ToLower().Contains(requestQuery.OpenText.ToLower())
                            || (obj.EmploymentType != null && obj.EmploymentType.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Email != null && obj.Email.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Phone != null && obj.Phone.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Department != null && obj.Department.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Designation != null && obj.Designation.ToLower().Contains(requestQuery.OpenText.ToLower()))
                            || (obj.Address != null && obj.Address.ToLower().Contains(requestQuery.OpenText.ToLower()));
        }

        Expression<Func<Employee, EmployeeListResponse>>? selector = x => new EmployeeListResponse(
               x.Id,
               x.EmployeeName,
               x.EmployeeCode,
               x.EmploymentType,
               x.Email,
               x.Phone,
               x.Address,
               x.DateOfBirth,
               x.JoiningDate,
               x.Department,
               x.Designation,
               x.Salary,
               x.EmergencyContact,
               x.BloodGroup,
               x.NationalId,
               x.PassportNumber,
               x.BankAccount,
               x.Notes,
               x.PhotoUrl,
               x.Status
            );

        return await _repository.PaginationQuery(paginationQuery: requestQuery, predicate: predicate, selector: selector, cancellationToken);
    }

    public async Task<string> GenerateCode(CancellationToken cancellationToken = default)
    {
        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();

        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            var branchId = _currentUser.BranchId;
            var lastEmployee = await _repository.Query()
                .Where(x => x.BranchId == branchId)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var nextNumber = lastEmployee != null ?
                ExtractNumberFromCode(lastEmployee.EmployeeCode) + 1 : 1;

            return $"EMP-{branchId:D3}-{nextNumber:D6}";
        }
        else
        {
            var lastEmployee = await _repository.Query()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var nextNumber = lastEmployee != null ?
                ExtractNumberFromCode(lastEmployee.EmployeeCode) + 1 : 1;

            return $"EMP-{nextNumber:D6}";
        }
    }

    private static int ExtractNumberFromCode(string code)
    {
        var parts = code.Split('-');
        return parts.Length > 0 && int.TryParse(parts[^1], out var number) ? number : 0;
    }

    public async Task<EmployeeResponse> UpdateAsync(int id, EmployeeRequest employee, CancellationToken cancellationToken = default)
    {
        var existingData = await _repository.GetByIdAsync(id, cancellationToken);
        if (existingData is null) throw new ArgumentNullException(nameof(existingData));

        var codeGenDependOn = await _companyRepository.Query().Select(x => x.CodeGeneration).FirstOrDefaultAsync();
        if (codeGenDependOn == ECodeGeneration.Branch)
        {
            EmployeeValidator validator = new(_repository, id);
            await validator.ValidateAndThrowAsync(employee, cancellationToken);
        }
        else
        {
            EmployeeValidator validator = new(_repository, id);
            await validator.ValidateAndThrowAsync(employee, cancellationToken);
        }

        _repository.UpdatableQuery(x => x.Id == id).ExecuteUpdate(setters =>
        setters.SetProperty(cmd => cmd.EmployeeName, employee.EmployeeName)
               .SetProperty(cmd => cmd.EmployeeCode, employee.EmployeeCode)
               .SetProperty(cmd => cmd.EmploymentType, employee.EmploymentType)
               .SetProperty(cmd => cmd.Email, employee.Email)
               .SetProperty(cmd => cmd.Phone, employee.Phone)
               .SetProperty(cmd => cmd.Address, employee.Address)
               .SetProperty(cmd => cmd.DateOfBirth, employee.DateOfBirth)
               .SetProperty(cmd => cmd.JoiningDate, employee.JoiningDate)
               .SetProperty(cmd => cmd.Department, employee.Department)
               .SetProperty(cmd => cmd.Designation, employee.Designation)
               .SetProperty(cmd => cmd.Salary, employee.Salary)
               .SetProperty(cmd => cmd.EmergencyContact, employee.EmergencyContact)
               .SetProperty(cmd => cmd.BloodGroup, employee.BloodGroup)
               .SetProperty(cmd => cmd.NationalId, employee.NationalId)
               .SetProperty(cmd => cmd.PassportNumber, employee.PassportNumber)
               .SetProperty(cmd => cmd.BankAccount, employee.BankAccount)
               .SetProperty(cmd => cmd.Notes, employee.Notes)
               .SetProperty(cmd => cmd.PhotoUrl, employee.PhotoUrl)
               .SetProperty(cmd => cmd.IsActive, employee.IsActive)
        );

        var response = employee.Adapt<EmployeeResponse>();
        return response;
    }

    public async Task<IEnumerable<string>> GetDistinctDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var distinctDepartments = await _repository.Query()
            .Where(x => !string.IsNullOrEmpty(x.Department))
            .Select(x => x.Department!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return distinctDepartments;
    }

    public async Task<IEnumerable<string>> GetDistinctDesignationsAsync(CancellationToken cancellationToken = default)
    {
        var distinctDesignations = await _repository.Query()
            .Where(x => !string.IsNullOrEmpty(x.Designation))
            .Select(x => x.Designation!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return distinctDesignations;
    }
}