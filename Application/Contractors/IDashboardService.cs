using Application.ReponseDTO;

namespace Application.Contractors;

public interface IDashboardService
{
    Task<DashboardStatsResponse> GetDashboardStatsAsync(DateTime startDate, DateTime endDate, int? branchId = null, CancellationToken cancellationToken = default);
    Task<DashboardTrendsResponse> GetDashboardTrendsAsync(int periodDays, int? branchId = null, CancellationToken cancellationToken = default);
}
