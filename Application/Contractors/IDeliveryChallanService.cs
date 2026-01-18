using Application.Framework;
using Application.RequestDTO;
using Application.ReponseDTO;

namespace Application.Contractors;

public interface IDeliveryChallanService
{
    Task<IEnumerable<DeliveryChallanListResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<PaginationResult<DeliveryChallanListResponse>> PaginationListAsync(PaginationQuery requestQuery, CancellationToken cancellationToken = default);
    Task<DeliveryChallanResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DeliveryChallanResponse> AddAsync(DeliveryChallanRequest request, CancellationToken cancellationToken = default);
    Task<DeliveryChallanResponse> UpdateAsync(Guid id, DeliveryChallanRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteBatchAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<string> GenerateChallanNumber(CancellationToken cancellationToken = default);
    Task<DeliveryChallanResponse> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
}
