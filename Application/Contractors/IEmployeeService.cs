using Application.RequestDTO;
using Application.ReponseDTO;
using Application.Framework;

namespace Application.Contractors;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<EmployeeListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> AddAsync(EmployeeRequest employee, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> UpdateAsync(int id, EmployeeRequest employee, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lookup<int>>> GetLookup(Expression<Func<Employee, bool>> predicate, CancellationToken cancellationToken = default);
    Task<string> GenerateCode(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetDistinctDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetDistinctDesignationsAsync(CancellationToken cancellationToken = default);
}