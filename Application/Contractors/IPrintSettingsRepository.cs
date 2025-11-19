using Domain.Entitites;

namespace Application.Contractors
{
    public interface IPrintSettingsRepository
    {
        Task<PrintSettings?> GetByBranchIdAsync(int branchId, CancellationToken cancellationToken = default);
        Task<PrintSettings> CreateAsync(PrintSettings printSettings, CancellationToken cancellationToken = default);
        Task<PrintSettings> UpdateAsync(PrintSettings printSettings, CancellationToken cancellationToken = default);
        Task<Branch?> GetBranchByIdAsync(int branchId, CancellationToken cancellationToken = default);
        Task<SupplierPayment?> GetSupplierPaymentByIdAsync(int paymentId, CancellationToken cancellationToken = default);
        Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    }
}