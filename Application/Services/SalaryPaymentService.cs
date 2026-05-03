using Application.Common;
using Application.Contractors;
using Application.Contractors.Authentication;
using Application.Framework;
using Application.ReponseDTO;
using Application.RequestDTO;
using Application.Validators;
using Domain;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class SalaryPaymentService : ISalaryPaymentService
{
    private const int EditWindowDays = 1;

    private readonly IRepository<Employee, int> _employeeRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<SalaryPayment, int> _salaryPaymentRepository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly ITenantProvider _tenantProvider;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly CurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SalaryPaymentValidator _validator;

    public SalaryPaymentService(
        IRepository<Employee, int> employeeRepository,
        IRepository<Transaction, Guid> transactionRepository,
        IRepository<SalaryPayment, int> salaryPaymentRepository,
        IRepository<TransactionHead, Guid> transactionHeadRepository,
        ICodeGenerationService codeGenerationService,
        ITenantProvider tenantProvider,
        DefaultValueInjector defaultValueInjector,
        IUserContextService userContextService,
        IUnitOfWork unitOfWork,
        SalaryPaymentValidator validator)
    {
        _employeeRepository = employeeRepository;
        _transactionRepository = transactionRepository;
        _salaryPaymentRepository = salaryPaymentRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _codeGenerationService = codeGenerationService;
        _tenantProvider = tenantProvider;
        _defaultValueInjector = defaultValueInjector;
        _currentUser = userContextService.GetCurrentUser();
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<IEnumerable<EmployeeForSalaryResponse>> GetEmployeesForPaymentAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && e.IsActive)
            .ToListAsync(cancellationToken);

        if (!employees.Any())
            return Enumerable.Empty<EmployeeForSalaryResponse>();

        var employeeIds = employees.Select(e => e.Id).ToList();

        // Single projection query — fetch only needed fields for active employees
        var lastPaymentData = await _salaryPaymentRepository.Query()
            .Include(sp => sp.Transaction)
            .Where(sp => sp.TenantId == tenantId
                      && !sp.Transaction!.IsDeleted
                      && employeeIds.Contains(sp.EmployeeId))
            .Select(sp => new
            {
                sp.EmployeeId,
                TransactionDate = sp.Transaction!.TransactionDate,
                sp.Month,
                sp.Year
            })
            .ToListAsync(cancellationToken);

        // Group in-memory to find the latest payment per employee
        var lastByEmployee = lastPaymentData
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.TransactionDate).First());

        return employees.Select(emp =>
        {
            lastByEmployee.TryGetValue(emp.Id, out var last);
            return new EmployeeForSalaryResponse(
                emp.Id,
                emp.EmployeeName,
                emp.EmployeeCode,
                emp.Designation ?? "",
                emp.Salary,
                last?.TransactionDate,
                last != null ? $"{last.Month:D2}/{last.Year}" : null
            );
        }).ToList();
    }

    public async Task<SalaryPaymentResponse> CreateSalaryPaymentAsync(SalaryPaymentRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var nextCode = await _codeGenerationService.GenerateCodeAsync(
            _transactionRepository.Query(),
            "SAL",
            t => t.TransactionCode,
            cancellationToken);

        var tenantId = _tenantProvider.GetTenantId();
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

        if (employee == null || employee.TenantId != tenantId)
            throw new NotFoundException("Employee not found");

        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(th => th.UsageFor == UsageFor.SALARY && th.IsActive && th.Type == TransactionHeadTypes.DEBIT, cancellationToken);
        if (transactionHead == null)
            throw new BusinessRuleException("Salary Transaction Head not configured");

        var netAmount = request.BasicSalary + request.Bonus - request.Deduction;
        var period = $"{request.Month:D2}/{request.Year}";

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = nextCode,
            TransactionDate = DateTime.UtcNow,
            TransactionHeadId = transactionHead.Id,
            EmployeeId = employee.Id,
            Amount = (-1) * netAmount,
            NetAmount = (-1) * netAmount,
            PaymentMethod = request.PaymentMethod,
            Description = $"Salary payment for {period}",
            Note = string.IsNullOrEmpty(request.Note) ? null : request.Note,
            BranchId = employee.BranchId ?? 1,
            TenantId = tenantId
        };

        var salaryPayment = new SalaryPayment
        {
            TransactionId = transaction.Id,
            EmployeeId = employee.Id,
            BasicSalary = request.BasicSalary,
            Bonus = request.Bonus,
            Deduction = request.Deduction,
            Month = request.Month,
            Year = request.Year,
            TenantId = tenantId
        };

        // Atomic: both records committed together or neither
        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(transaction);
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            _defaultValueInjector.InjectCreatingAudit<SalaryPayment, int>(salaryPayment);
            await _salaryPaymentRepository.AddAsync(salaryPayment, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new SalaryPaymentResponse(
            salaryPayment.Id,
            employee.Id,
            employee.EmployeeName,
            employee.EmployeeCode,
            request.Month,
            request.Year,
            request.BasicSalary,
            request.Bonus,
            request.Deduction,
            netAmount,
            transaction.TransactionDate,
            request.PaymentMethod,
            request.Note,
            transaction.Id.ToString(),
            transaction.TransactionCode,
            transaction.CreatedTime
        );
    }

    public async Task<IEnumerable<SalaryPaymentListResponse>> GetSalaryPaymentListAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var salaryPayments = await _salaryPaymentRepository.Query()
            .Include(sp => sp.Transaction)
            .Include(sp => sp.Employee)
            .Where(sp => sp.TenantId == tenantId && !sp.Transaction!.IsDeleted)
            .OrderByDescending(sp => sp.Transaction!.TransactionDate)
            .ToListAsync(cancellationToken);

        return salaryPayments.Select(MapToListResponse).ToList();
    }

    public async Task<PaginationResult<SalaryPaymentListResponse>> PaginationListAsync(
        PaginationQuery requestQuery,
        int? employeeId,
        int? month,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var query = _salaryPaymentRepository.Query()
            .Include(sp => sp.Transaction)
            .Include(sp => sp.Employee)
            .Where(sp => sp.TenantId == tenantId && !sp.Transaction!.IsDeleted);

        if (employeeId.HasValue && employeeId.Value > 0)
            query = query.Where(sp => sp.EmployeeId == employeeId.Value);

        if (month.HasValue && month.Value > 0)
            query = query.Where(sp => sp.Month == month.Value);

        if (year.HasValue && year.Value > 0)
            query = query.Where(sp => sp.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchTerm = requestQuery.OpenText.ToLower();
            query = query.Where(sp =>
                sp.Employee!.EmployeeName.ToLower().Contains(searchTerm) ||
                sp.Employee.EmployeeCode.ToLower().Contains(searchTerm) ||
                sp.Transaction!.TransactionCode.ToLower().Contains(searchTerm));
        }

        query = requestQuery.OrderBy switch
        {
            "employeeName" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.Employee!.EmployeeName)
                : query.OrderByDescending(sp => sp.Employee!.EmployeeName),
            "employeeCode" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.Employee!.EmployeeCode)
                : query.OrderByDescending(sp => sp.Employee!.EmployeeCode),
            "transactionCode" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.Transaction!.TransactionCode)
                : query.OrderByDescending(sp => sp.Transaction!.TransactionCode),
            "basicSalary" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.BasicSalary)
                : query.OrderByDescending(sp => sp.BasicSalary),
            "netAmount" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.Transaction!.NetAmount)
                : query.OrderByDescending(sp => sp.Transaction!.NetAmount),
            "paymentMethod" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.Transaction!.PaymentMethod)
                : query.OrderByDescending(sp => sp.Transaction!.PaymentMethod),
            "createdTime" => requestQuery.IsAscending ?? true
                ? query.OrderBy(sp => sp.Transaction!.CreatedTime)
                : query.OrderByDescending(sp => sp.Transaction!.CreatedTime),
            _ => query.OrderByDescending(sp => sp.Transaction!.TransactionDate)
        };

        // Count the full result set before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch only the current page
        var items = await query
            .Skip(requestQuery.PageIndex * requestQuery.PageSize)
            .Take(requestQuery.PageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(MapToListResponse).ToList();

        // Use pre-fetched factory to avoid re-counting the page
        return PaginationResult<SalaryPaymentListResponse>.Create(mapped, requestQuery.PageIndex, requestQuery.PageSize, totalCount);
    }

    public async Task<IEnumerable<SalaryPaymentListResponse>> GetPaymentHistoryAsync(
        int? employeeId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _salaryPaymentRepository.Query()
            .Include(sp => sp.Transaction)
            .Include(sp => sp.Employee)
            .Where(sp => sp.TenantId == tenantId && !sp.Transaction!.IsDeleted);

        if (employeeId.HasValue)
            query = query.Where(sp => sp.EmployeeId == employeeId.Value);

        if (startDate.HasValue)
            query = query.Where(sp => sp.Transaction!.TransactionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sp => sp.Transaction!.TransactionDate <= endDate.Value);

        var results = await query.OrderByDescending(sp => sp.Transaction!.TransactionDate).ToListAsync(cancellationToken);
        return results.Select(MapToListResponse).ToList();
    }

    public async Task<MonthlyPaymentSummaryResponse> GetMonthlyPaymentReportAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var salaryPayments = await _salaryPaymentRepository.Query()
            .Include(sp => sp.Transaction)
            .Include(sp => sp.Employee)
            .Where(sp => sp.TenantId == tenantId
                && !sp.Transaction!.IsDeleted
                && sp.Month == month
                && sp.Year == year)
            .ToListAsync(cancellationToken);

        var payments = salaryPayments.Select(MapToListResponse).ToList();

        return new MonthlyPaymentSummaryResponse
        {
            Month = month,
            Year = year,
            TotalEmployees = payments.Count,
            TotalBasicSalary = salaryPayments.Sum(sp => sp.BasicSalary),
            TotalBonus = salaryPayments.Sum(sp => sp.Bonus),
            TotalDeduction = salaryPayments.Sum(sp => sp.Deduction),
            TotalNetAmount = payments.Sum(p => p.NetAmount),
            Payments = payments
        };
    }

    public async Task<SalaryPaymentResponse> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var sp = await _salaryPaymentRepository.Query()
            .Include(x => x.Transaction).ThenInclude(t => t!.TransactionHead)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId && x.TenantId == tenantId, cancellationToken);

        if (sp == null || sp.Transaction == null || sp.Transaction.IsDeleted)
            throw new NotFoundException("Salary payment not found");

        return ToSalaryPaymentResponse(sp);
    }

    public async Task<SalaryPaymentResponse> UpdateSalaryPaymentAsync(Guid transactionId, SalaryPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        var sp = await _salaryPaymentRepository.Query()
            .Include(x => x.Transaction).ThenInclude(t => t!.TransactionHead)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId && x.TenantId == tenantId, cancellationToken);

        if (sp == null || sp.Transaction == null || sp.Transaction.IsDeleted ||
            sp.Transaction.TransactionHead?.UsageFor != UsageFor.SALARY)
            throw new NotFoundException("Salary payment not found");

        if (sp.Transaction.CreatedTime < DateTime.UtcNow.AddDays(-EditWindowDays))
            throw new BusinessRuleException($"Cannot update salary payment. Updates are only allowed within {EditWindowDays} day(s) of creation.");

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null || employee.TenantId != tenantId)
            throw new NotFoundException("Employee not found");

        var netAmount = request.BasicSalary + request.Bonus - request.Deduction;
        var period = $"{request.Month:D2}/{request.Year}";

        sp.Transaction.EmployeeId = employee.Id;
        sp.Transaction.Amount = (-1) * netAmount;
        sp.Transaction.NetAmount = (-1) * netAmount;
        sp.Transaction.PaymentMethod = request.PaymentMethod;
        sp.Transaction.Description = $"Salary payment for {period}";
        sp.Transaction.Note = string.IsNullOrEmpty(request.Note) ? null : request.Note;

        sp.EmployeeId = employee.Id;
        sp.BasicSalary = request.BasicSalary;
        sp.Bonus = request.Bonus;
        sp.Deduction = request.Deduction;
        sp.Month = request.Month;
        sp.Year = request.Year;

        // Atomic: both records updated together or neither
        await using var dbTransaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _defaultValueInjector.InjectUpdatingAudit<Transaction, Guid>(sp.Transaction);
            await _transactionRepository.UpdateAsync(sp.Transaction, cancellationToken);

            _defaultValueInjector.InjectUpdatingAudit<SalaryPayment, int>(sp);
            await _salaryPaymentRepository.UpdateAsync(sp, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new SalaryPaymentResponse(
            sp.Id,
            employee.Id,
            employee.EmployeeName,
            employee.EmployeeCode,
            request.Month,
            request.Year,
            request.BasicSalary,
            request.Bonus,
            request.Deduction,
            netAmount,
            sp.Transaction.TransactionDate,
            request.PaymentMethod,
            request.Note,
            sp.TransactionId.ToString(),
            sp.Transaction.TransactionCode,
            sp.Transaction.CreatedTime
        );
    }

    public async Task<bool> DeleteSalaryPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transaction = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null || transaction.TenantId != tenantId ||
            transaction.TransactionHead?.UsageFor != UsageFor.SALARY)
            throw new NotFoundException("Salary payment not found");

        if (transaction.CreatedTime < DateTime.UtcNow.AddDays(-EditWindowDays))
            throw new BusinessRuleException($"Cannot delete salary payment. Deletion is only allowed within {EditWindowDays} day(s) of creation.");

        transaction.IsDeleted = true;
        transaction.DeletedAt = DateTime.UtcNow;
        transaction.DeletedById = _currentUser.Id;
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        return true;
    }

    // --- Helpers ---

    public async Task<IEnumerable<Lookup<string>>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        return await _salaryPaymentRepository.Query()
            .Include(sp => sp.Transaction)
            .Include(sp => sp.Employee)
            .Where(sp => sp.TenantId == tenantId && !sp.Transaction!.IsDeleted)
            .OrderByDescending(sp => sp.Transaction!.TransactionDate)
            .Select(sp => new Lookup<string>(
                sp.TransactionId.ToString(),
                sp.Employee!.EmployeeName + " (" + sp.Month.ToString("D2") + "/" + sp.Year + ") - " + sp.Transaction!.TransactionCode
            ))
            .ToListAsync(cancellationToken);
    }

    private static SalaryPaymentListResponse MapToListResponse(SalaryPayment sp) =>
        new SalaryPaymentListResponse(
            sp.TransactionId,
            sp.Employee?.EmployeeName ?? "Unknown",
            sp.Employee?.EmployeeCode ?? "N/A",
            $"{sp.Month:D2}/{sp.Year}",
            sp.BasicSalary,
            Math.Abs(sp.Transaction?.NetAmount ?? 0),
            sp.Transaction?.TransactionDate ?? DateTime.UtcNow,
            sp.Transaction?.PaymentMethod ?? "",
            sp.Transaction?.CreatedTime ?? DateTime.UtcNow
        );

    private static SalaryPaymentResponse ToSalaryPaymentResponse(SalaryPayment sp) =>
        new SalaryPaymentResponse(
            sp.Id,
            sp.EmployeeId,
            sp.Employee?.EmployeeName ?? "Unknown",
            sp.Employee?.EmployeeCode ?? "N/A",
            sp.Month,
            sp.Year,
            sp.BasicSalary,
            sp.Bonus,
            sp.Deduction,
            Math.Abs(sp.Transaction?.NetAmount ?? 0),
            sp.Transaction?.TransactionDate ?? DateTime.UtcNow,
            sp.Transaction?.PaymentMethod ?? "",
            sp.Transaction?.Note,
            sp.TransactionId.ToString(),
            sp.Transaction?.TransactionCode ?? "",
            sp.Transaction?.CreatedTime ?? DateTime.UtcNow
        );
}
