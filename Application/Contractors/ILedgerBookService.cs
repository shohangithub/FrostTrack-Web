using Application.ReponseDTO;

namespace Application.Contractors
{
    public interface ILedgerBookService
    {
        Task<LedgerBookSummaryResponse> GetSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<LedgerBookGroupResponse> GetGroupLedgerAsync(Guid groupId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    }
}
