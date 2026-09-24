using Application.ReponseDTO;

namespace Application.Contractors;

public interface ISeasonArchiveService
{
    Task<SeasonArchivePreviewResponse> GetArchivePreviewAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    Task<SeasonArchiveResultResponse> ExecuteArchiveAsync(ExecuteSeasonArchiveRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SeasonArchiveHistoryResponse>> GetArchiveHistoryAsync(CancellationToken cancellationToken = default);
}
