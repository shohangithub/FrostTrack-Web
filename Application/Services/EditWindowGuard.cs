namespace Application.Services;

public class EditWindowGuard : IEditWindowGuard
{
    private const int DEFAULT_ALLOWED_DAYS = 90;

    private readonly IRepository<Company, int> _companyRepository;
    private readonly IRepository<Branch, int> _branchRepository;
    private readonly CurrentUser _currentUser;
    private EditWindowPolicyResponse? _policy;

    public EditWindowGuard(
        IRepository<Company, int> companyRepository,
        IRepository<Branch, int> branchRepository,
        IUserContextService userContextService)
    {
        _companyRepository = companyRepository;
        _branchRepository = branchRepository;
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<EditWindowPolicyResponse> GetPolicyAsync(CancellationToken cancellationToken = default)
    {
        if (_policy != null) return _policy;

        var companyId = await _branchRepository.Query()
            .Where(b => b.Id == _currentUser.BranchId)
            .Select(b => b.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        var company = companyId.HasValue
            ? await _companyRepository.Query().FirstOrDefaultAsync(c => c.Id == companyId.Value, cancellationToken)
            : await _companyRepository.Query().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);

        _policy = company == null
            ? new EditWindowPolicyResponse(true, DEFAULT_ALLOWED_DAYS)
            : new EditWindowPolicyResponse(
                company.RestrictEditDelete,
                company.EditDeleteAllowedDays > 0 ? company.EditDeleteAllowedDays : DEFAULT_ALLOWED_DAYS);

        return _policy;
    }

    public async Task EnsureEditableAsync(DateTime recordDate, string recordName, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyAsync(cancellationToken);
        if (!policy.RestrictEditDelete) return;

        var cutoff = DateTime.UtcNow.Date.AddDays(-policy.EditDeleteAllowedDays);
        var date = recordDate.Kind == DateTimeKind.Local ? recordDate.ToUniversalTime() : recordDate;

        if (date.Date < cutoff)
            throw new Exception($"This {recordName} is dated {date:dd MMM yyyy}, which is older than {policy.EditDeleteAllowedDays} days, so it can no longer be modified or deleted.");
    }
}
