namespace Application.Contractors;

public interface ICompanyService
{
    Task<IEnumerable<CompanyListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<CompanyResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CompanyResponse> AddAsync(CompanyRequest request, CancellationToken cancellationToken = default);
    Task<CompanyResponse> UpdateAsync(int id, CompanyRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<PaginationResult<CompanyListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
}
