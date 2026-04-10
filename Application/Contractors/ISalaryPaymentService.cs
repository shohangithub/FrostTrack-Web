using Application.ReponseDTO;

namespace Application.Contractors;

public interface ISalaryPaymentService
{
    Task<IEnumerable<EmployeeForSalaryResponse>> GetEmployeesForPaymentAsync(CancellationToken cancellationToken = default);
    Task<SalaryPaymentResponse> CreateSalaryPaymentAsync(RequestDTO.SalaryPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaginationResult<SalaryPaymentListResponse>> PaginationListAsync(PaginationQuery requestQuery, int? employeeId, int? month, int? year, CancellationToken cancellationToken = default);
    Task<IEnumerable<SalaryPaymentListResponse>> GetSalaryPaymentListAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SalaryPaymentListResponse>> GetPaymentHistoryAsync(int? employeeId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<MonthlyPaymentSummaryResponse> GetMonthlyPaymentReportAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<SalaryPaymentResponse> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<SalaryPaymentResponse> UpdateSalaryPaymentAsync(Guid id, RequestDTO.SalaryPaymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSalaryPaymentAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<string>>> GetLookupAsync(CancellationToken cancellationToken = default);
}
