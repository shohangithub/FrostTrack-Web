namespace Application.Contractors;

public interface IBalanceSheetEntryService
{
    Task<IEnumerable<BalanceSheetEntryResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<BalanceSheetEntryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BalanceSheetEntryResponse?> AddAsync(BalanceSheetEntryRequest request, CancellationToken cancellationToken = default);
    Task<BalanceSheetEntryResponse?> UpdateAsync(Guid id, BalanceSheetEntryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BalanceSheetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<BalanceSheetSettingsResponse> UpdateSettingsAsync(BalanceSheetSettingsRequest request, CancellationToken cancellationToken = default);
}
