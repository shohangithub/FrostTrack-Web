using Application.Contractors;
using Application.Contractors.Authentication;
using Application.ReponseDTO;
using Application.RequestDTO;
using Domain;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class SalaryPaymentService : ISalaryPaymentService
{
    private readonly IRepository<Employee, int> _employeeRepository;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    private readonly IRepository<TransactionHead, Guid> _transactionHeadRepository;
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly ITenantProvider _tenantProvider;
    private readonly DefaultValueInjector _defaultValueInjector;
    private readonly CurrentUser _currentUser;

    public SalaryPaymentService(
        IRepository<Employee, int> employeeRepository,
        IRepository<Transaction, Guid> transactionRepository,
         IRepository<TransactionHead, Guid> transactionHeadRepository,
        ICodeGenerationService codeGenerationService,
        ITenantProvider tenantProvider,
        DefaultValueInjector defaultValueInjector,
        IUserContextService userContextService)
    {
        _employeeRepository = employeeRepository;
        _transactionRepository = transactionRepository;
        _transactionHeadRepository = transactionHeadRepository;
        _codeGenerationService = codeGenerationService;
        _tenantProvider = tenantProvider;
        _defaultValueInjector = defaultValueInjector;
        _currentUser = userContextService.GetCurrentUser();
    }

    public async Task<IEnumerable<EmployeeForSalaryResponse>> GetEmployeesForPaymentAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && e.IsActive)
            .ToListAsync(cancellationToken);

        var transactions = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == tenantId && t.TransactionHead!.UsageFor == UsageFor.SALARY)
            .ToListAsync(cancellationToken);

        var result = employees.Select(emp =>
        {
            var lastPayment = transactions
                .Where(t => t.EntityId == emp.Id.ToString())
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            return new EmployeeForSalaryResponse(
                emp.Id,
                emp.EmployeeName,
                emp.EmployeeCode,
                emp.Designation ?? "",
                emp.Salary,
                lastPayment?.TransactionDate,
                lastPayment?.Note
            );
        }).ToList();

        return result;
    }



    public async Task<SalaryPaymentResponse> CreateSalaryPaymentAsync(SalaryPaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Generate transaction code
        var nextCode = await _codeGenerationService.GenerateCodeAsync(
            _transactionRepository.Query(),
            "SAL",
            t => t.TransactionCode,
            cancellationToken);

        var validator = new SalaryPaymentValidator(_employeeRepository, _transactionRepository, nextCode);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var tenantId = _tenantProvider.GetTenantId();
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

        if (employee == null || employee.TenantId != tenantId)
        {
            throw new Exception("Employee not found");
        }

        var netAmount = request.BasicSalary + request.Bonus - request.Deduction;
        var period = $"{request.Month:D2}/{request.Year}";

        var transactionHead = await _transactionHeadRepository.Query()
            .FirstOrDefaultAsync(th => th.UsageFor == UsageFor.SALARY && th.IsActive && th.Type == TransactionHeadTypes.DEBIT, cancellationToken);
        if (transactionHead == null)
        {
            throw new Exception("Salary Transaction Head not configured");
        }
        // Create transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = nextCode,
            TransactionDate = DateTime.UtcNow,
            TransactionHeadId = transactionHead.Id,
            EntityName = "Employee",
            EntityId = employee.Id.ToString(),
            EmployeeId = employee.Id, // Add explicit EmployeeId
            Amount = (-1) * netAmount,
            NetAmount = (-1) * netAmount,
            PaymentMethod = request.PaymentMethod,
            Description = $"Salary payment for {period}",
            Note = string.IsNullOrEmpty(request.Note) ? null : request.Note,
            BranchId = employee.BranchId ?? 1,
            TenantId = tenantId
        };


        _defaultValueInjector.InjectCreatingAudit<Transaction, Guid>(transaction);
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        var response = new SalaryPaymentResponse(
            0,
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
            DateTime.Now
        );

        return response;
    }

    public async Task<IEnumerable<SalaryPaymentListResponse>> GetSalaryPaymentListAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transactions = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == tenantId && t.TransactionHead!.UsageFor == UsageFor.SALARY)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        var employeeIds = transactions.Select(t => int.Parse(t.EntityId)).Distinct().ToList();
        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && employeeIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var employeeDict = employees.ToDictionary(e => e.Id.ToString(), e => e);

        var result = transactions.Select(t =>
        {
            var emp = employeeDict.ContainsKey(t.EntityId) ? employeeDict[t.EntityId] : null;
            var period = ExtractPeriodFromNote(t.Description ?? "");

            return new SalaryPaymentListResponse(
                t.Id,
                emp?.EmployeeName ?? "Unknown",
                emp?.EmployeeCode ?? "N/A",
                period,
                t.Amount,
                t.NetAmount,
                t.TransactionDate,
                t.PaymentMethod,
                t.CreatedTime
            );
        }).ToList();

        return result;
    }

    public async Task<PaginationResult<SalaryPaymentListResponse>> PaginationListAsync(
        PaginationQuery requestQuery,
        int? employeeId,
        int? month,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();

        // Build base query
        var query = _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == tenantId && t.TransactionHead!.UsageFor == UsageFor.SALARY && !t.IsDeleted);

        // Apply employee filter
        if (employeeId.HasValue && employeeId.Value > 0)
        {
            query = query.Where(t => t.EntityId == employeeId.Value.ToString());
        }

        // Apply month filter
        if (month.HasValue && month.Value > 0)
        {
            query = query.Where(t => t.TransactionDate.Month == month.Value);
        }

        // Apply year filter
        if (year.HasValue && year.Value > 0)
        {
            query = query.Where(t => t.TransactionDate.Year == year.Value);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(requestQuery.OpenText) && !string.IsNullOrWhiteSpace(requestQuery.OpenText))
        {
            var searchTerm = requestQuery.OpenText.ToLower();
            var employeeIds = await _employeeRepository.Query()
                .Where(e => e.TenantId == tenantId &&
                    (e.EmployeeName.ToLower().Contains(searchTerm) ||
                     e.EmployeeCode.ToLower().Contains(searchTerm)))
                .Select(e => e.Id.ToString())
                .ToListAsync(cancellationToken);

            query = query.Where(t => employeeIds.Contains(t.EntityId) ||
                                    t.Description.ToLower().Contains(searchTerm) ||
                                    t.TransactionCode.ToLower().Contains(searchTerm));
        }

        // Apply sorting - map frontend column names to entity properties
        if (!string.IsNullOrEmpty(requestQuery.OrderBy))
        {
            var orderByColumn = requestQuery.OrderBy switch
            {
                "transactionCode" => nameof(Transaction.TransactionCode),
                "transactionDate" => nameof(Transaction.TransactionDate),
                "amount" => nameof(Transaction.Amount),
                "netAmount" => nameof(Transaction.NetAmount),
                "paymentMethod" => nameof(Transaction.PaymentMethod),
                "employeeCode" => nameof(Transaction.EntityId), // Will sort by EntityId (employee id)
                "employeeName" => nameof(Transaction.EntityId), // Will sort by EntityId (employee id)
                "period" => nameof(Transaction.TransactionDate), // Sort by transaction date for period
                "createdTime" => nameof(Transaction.CreatedTime),
                "basicSalary" => nameof(Transaction.Amount),
                _ => nameof(Transaction.TransactionDate) // Default to TransactionDate
            };

            query = requestQuery.IsAscending ?? true
                ? query.OrderBy(t => EF.Property<object>(t, orderByColumn))
                : query.OrderByDescending(t => EF.Property<object>(t, orderByColumn));
        }
        else
        {
            query = query.OrderByDescending(t => t.TransactionDate);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paged data
        var transactions = await query
            .Skip(requestQuery.PageIndex * requestQuery.PageSize)
            .Take(requestQuery.PageSize)
            .ToListAsync(cancellationToken);

        // Get employee details
        var empIds = transactions.Select(t => int.Parse(t.EntityId)).Distinct().ToList();
        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && empIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var employeeDict = employees.ToDictionary(e => e.Id.ToString(), e => e);

        // Map to response
        var result = transactions.Select(t =>
        {
            var emp = employeeDict.ContainsKey(t.EntityId) ? employeeDict[t.EntityId] : null;
            var period = ExtractPeriodFromNote(t.Description ?? "");

            return new SalaryPaymentListResponse(
                    t.Id,
                    emp?.EmployeeName ?? "Unknown",
                    emp?.EmployeeCode ?? "N/A",
                    period,
                    t.Amount,
                    t.NetAmount,
                    t.TransactionDate,
                    t.PaymentMethod,
                    t.CreatedTime
                );
        }).AsQueryable();

        return await PaginationResult<SalaryPaymentListResponse>.CreateAsync(
            result,
            requestQuery.PageIndex,
            requestQuery.PageSize,
            cancellationToken
        );
    }

    public async Task<IEnumerable<SalaryPaymentListResponse>> GetPaymentHistoryAsync(
        int? employeeId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == tenantId && t.TransactionHead!.UsageFor == UsageFor.SALARY);

        if (employeeId.HasValue)
        {
            query = query.Where(t => t.EntityId == employeeId.Value.ToString());
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync(cancellationToken);

        var employeeIds = transactions.Select(t => int.Parse(t.EntityId)).Distinct().ToList();
        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && employeeIds.Contains(e.Id))
            .ToListAsync(cancellationToken); var employeeDict = employees.ToDictionary(e => e.Id.ToString(), e => e);

        var result = transactions.Select(t =>
        {
            var emp = employeeDict.ContainsKey(t.EntityId) ? employeeDict[t.EntityId] : null;
            var period = ExtractPeriodFromNote(t.Description ?? "");

            return new SalaryPaymentListResponse(
                Guid.Empty,
                emp?.EmployeeName ?? "Unknown",
                emp?.EmployeeCode ?? "N/A",
                period,
                t.Amount,
                t.NetAmount,
                t.TransactionDate,
                t.PaymentMethod,
                t.CreatedTime
            );
        }).ToList();

        return result;
    }

    public async Task<MonthlyPaymentSummaryResponse> GetMonthlyPaymentReportAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .Where(t => t.TenantId == tenantId
                && t.TransactionHead!.UsageFor == UsageFor.SALARY
                && t.TransactionDate >= startDate
                && t.TransactionDate <= endDate)
            .ToListAsync(cancellationToken);

        var employeeIds = transactions.Select(t => int.Parse(t.EntityId)).Distinct().ToList();
        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && employeeIds.Contains(e.Id))
            .ToListAsync(cancellationToken); var employeeDict = employees.ToDictionary(e => e.Id.ToString(), e => e);

        var payments = transactions.Select(t =>
        {
            var emp = employeeDict.ContainsKey(t.EntityId) ? employeeDict[t.EntityId] : null;
            var period = $"{month:D2}/{year}";

            return new SalaryPaymentListResponse(
                Guid.Empty,
                emp?.EmployeeName ?? "Unknown",
                emp?.EmployeeCode ?? "N/A",
                period,
                t.Amount,
                t.NetAmount,
                t.TransactionDate,
                t.PaymentMethod,
                t.CreatedTime
            );
        }).ToList();

        return new MonthlyPaymentSummaryResponse
        {
            Month = month,
            Year = year,
            TotalEmployees = payments.Count,
            TotalBasicSalary = payments.Sum(p => p.BasicSalary),
            TotalBonus = 0,
            TotalDeduction = 0,
            TotalNetAmount = payments.Sum(p => p.NetAmount),
            Payments = payments
        };
    }

    public async Task<SalaryPaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transaction = await _transactionRepository.Query().Include(t => t.TransactionHead)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null || transaction.TenantId != tenantId || transaction.TransactionHead!.UsageFor != UsageFor.SALARY)
        {
            throw new Exception("Salary payment not found");
        }

        var employee = await _employeeRepository.GetByIdAsync(int.Parse(transaction.EntityId), cancellationToken);

        var (month, year) = ExtractMonthYearFromNote(transaction.Description ?? "");

        return new SalaryPaymentResponse(
            0,
            employee?.Id ?? 0,
            employee?.EmployeeName ?? "Unknown",
            employee?.EmployeeCode ?? "N/A",
            month,
            year,
            transaction.Amount,
            0,
            0,
            transaction.NetAmount,
            transaction.TransactionDate,
            transaction.PaymentMethod,
            transaction.Note,
            transaction.Id.ToString(),
            transaction.CreatedTime
        );
    }

    private string ExtractPeriodFromNote(string note)
    {
        if (string.IsNullOrEmpty(note)) return "N/A";

        var match = System.Text.RegularExpressions.Regex.Match(note, @"(\d{2})/(\d{4})");
        return match.Success ? match.Value : "N/A";
    }

    private (int month, int year) ExtractMonthYearFromNote(string note)
    {
        if (string.IsNullOrEmpty(note)) return (DateTime.UtcNow.Month, DateTime.UtcNow.Year);

        var match = System.Text.RegularExpressions.Regex.Match(note, @"(\d{2})/(\d{4})");
        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        return (DateTime.UtcNow.Month, DateTime.UtcNow.Year);
    }

    public async Task<SalaryPaymentResponse> UpdateSalaryPaymentAsync(Guid id, SalaryPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transaction = await _transactionRepository.Query()
            .Include(t => t.TransactionHead)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null || transaction.TenantId != tenantId || transaction.TransactionHead!.UsageFor != UsageFor.SALARY)
        {
            throw new Exception("Salary payment not found");
        }

        // Check if the transaction was created within the last day
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        if (transaction.CreatedTime < oneDayAgo)
        {
            throw new Exception("Cannot update salary payment. Updates are only allowed within one day of creation.");
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null || employee.TenantId != tenantId)
        {
            throw new Exception("Employee not found");
        }

        var netAmount = request.BasicSalary + request.Bonus - request.Deduction;
        var period = $"{request.Month:D2}/{request.Year}";

        // Update transaction
        transaction.EntityId = employee.Id.ToString();
        transaction.EmployeeId = employee.Id;
        transaction.Amount = netAmount;
        transaction.NetAmount = netAmount;
        transaction.PaymentMethod = request.PaymentMethod;
        transaction.Description = $"Salary payment for {period}";
        transaction.Note = string.IsNullOrEmpty(request.Note) ? null : request.Note;

        _defaultValueInjector.InjectUpdatingAudit<Transaction, Guid>(transaction);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        return new SalaryPaymentResponse(
            0,
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
            transaction.CreatedTime
        );
    }

    public async Task<bool> DeleteSalaryPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transaction = _transactionRepository.Query().Include(t => t.TransactionHead).FirstOrDefault(t => t.Id == transactionId);

        if (transaction == null || transaction.TenantId != tenantId || transaction.TransactionHead!.UsageFor != UsageFor.SALARY)
        {
            throw new Exception("Salary payment not found");
        }

        // Check if the transaction was created within the last day
        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        if (transaction.CreatedTime < oneDayAgo)
        {
            throw new Exception("Cannot delete salary payment. Deletion is only allowed within one day of creation.");
        }

        // Soft delete the transaction
        transaction.IsDeleted = true;
        transaction.DeletedAt = DateTime.UtcNow;
        transaction.DeletedById = _currentUser.Id;

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        return true;
    }
}
