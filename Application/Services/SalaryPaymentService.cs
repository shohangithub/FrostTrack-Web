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
    private readonly ITenantProvider _tenantProvider;
    private readonly DefaultValueInjector _defaultValueInjector;

    public SalaryPaymentService(
        IRepository<Employee, int> employeeRepository,
        IRepository<Transaction, Guid> transactionRepository,
        ITenantProvider tenantProvider,
        DefaultValueInjector defaultValueInjector)
    {
        _employeeRepository = employeeRepository;
        _transactionRepository = transactionRepository;
        _tenantProvider = tenantProvider;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<IEnumerable<EmployeeForSalaryResponse>> GetEmployeesForPaymentAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var employees = await _employeeRepository.Query()
            .Where(e => e.TenantId == tenantId && e.IsActive)
            .ToListAsync(cancellationToken);

        var transactions = await _transactionRepository.Query()
            .Where(t => t.TenantId == tenantId && t.TransactionType == "SALARY")
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
        var tenantId = _tenantProvider.GetTenantId();
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

        if (employee == null || employee.TenantId != tenantId)
        {
            throw new Exception("Employee not found");
        }

        var netAmount = request.BasicSalary + request.Bonus - request.Deduction;
        var period = $"{request.Month:D2}/{request.Year}";

        // Get next transaction code
        var lastTransaction = await _transactionRepository.Query()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.TransactionCode)
            .FirstOrDefaultAsync(cancellationToken);

        var nextCode = "TRX-0001";
        if (lastTransaction != null && lastTransaction.TransactionCode.StartsWith("TRX-"))
        {
            var lastNumber = int.Parse(lastTransaction.TransactionCode.Substring(4));
            nextCode = $"TRX-{(lastNumber + 1):D4}";
        }

        // Create transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            TransactionCode = nextCode,
            TransactionDate = DateTime.Now,
            TransactionType = "SALARY",
            TransactionFlow = "OUT",
            EntityName = "Employee",
            EntityId = employee.Id.ToString(),
            Amount = netAmount,
            NetAmount = netAmount,
            PaymentMethod = request.PaymentMethod,
            Category = "SALARY",
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
        var transactions = await _transactionRepository.Query()
            .Where(t => t.TenantId == tenantId && t.TransactionType == "SALARY")
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
                0,
                emp?.EmployeeName ?? "Unknown",
                emp?.EmployeeCode ?? "N/A",
                period,
                t.Amount,
                t.NetAmount,
                t.TransactionDate,
                t.PaymentMethod
            );
        }).ToList();

        return result;
    }

    public async Task<IEnumerable<SalaryPaymentListResponse>> GetPaymentHistoryAsync(
        int? employeeId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var query = _transactionRepository.Query()
            .Where(t => t.TenantId == tenantId && t.TransactionType == "SALARY");

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
                0,
                emp?.EmployeeName ?? "Unknown",
                emp?.EmployeeCode ?? "N/A",
                period,
                t.Amount,
                t.NetAmount,
                t.TransactionDate,
                t.PaymentMethod
            );
        }).ToList();

        return result;
    }

    public async Task<MonthlyPaymentSummaryResponse> GetMonthlyPaymentReportAsync(int month, int year, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = await _transactionRepository.Query()
            .Where(t => t.TenantId == tenantId
                && t.TransactionType == "SALARY"
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
                0,
                emp?.EmployeeName ?? "Unknown",
                emp?.EmployeeCode ?? "N/A",
                period,
                t.Amount,
                t.NetAmount,
                t.TransactionDate,
                t.PaymentMethod
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

    public async Task<SalaryPaymentResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var transaction = await _transactionRepository.GetByIdAsync(Guid.Parse(id.ToString()), cancellationToken);

        if (transaction == null || transaction.TenantId != tenantId || transaction.TransactionType != "SALARY")
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
        if (string.IsNullOrEmpty(note)) return (DateTime.Now.Month, DateTime.Now.Year);

        var match = System.Text.RegularExpressions.Regex.Match(note, @"(\d{2})/(\d{4})");
        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        return (DateTime.Now.Month, DateTime.Now.Year);
    }
}
