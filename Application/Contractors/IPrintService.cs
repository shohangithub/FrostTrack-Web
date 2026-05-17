using Application.ReponseDTO;
using Application.RequestDTO;

namespace Application.Contractors
{
    public interface IPrintService
    {
        Task<PrintSettingsResponse> GetPrintSettingsByBranchAsync(int branchId, CancellationToken cancellationToken = default);
        Task<PrintSettingsResponse> CreateOrUpdatePrintSettingsAsync(int branchId, PrintSettingsResponse settings, CancellationToken cancellationToken = default);
        Task<BookingInvoiceData> GetBookingInvoiceDataAsync(Guid bookingId, CancellationToken cancellationToken = default);
        Task<string> GenerateBookingInvoiceHtmlAsync(BookingInvoiceData bookingData, PrintSettingsResponse printSettings, CancellationToken cancellationToken = default);
    }
}