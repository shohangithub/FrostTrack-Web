using Application.Contractors;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories
{
    public class PrintSettingsRepository : IPrintSettingsRepository
    {
        private readonly ApplicationDbContext _context;

        public PrintSettingsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PrintSettings?> GetByBranchIdAsync(int branchId, CancellationToken cancellationToken = default)
        {
            return await _context.PrintSettings
                .Include(p => p.Branch)
                .FirstOrDefaultAsync(p => p.BranchId == branchId, cancellationToken);
        }

        public async Task<PrintSettings> CreateAsync(PrintSettings printSettings, CancellationToken cancellationToken = default)
        {
            _context.PrintSettings.Add(printSettings);
            await _context.SaveChangesAsync(cancellationToken);
            return printSettings;
        }

        public async Task<PrintSettings> UpdateAsync(PrintSettings printSettings, CancellationToken cancellationToken = default)
        {
            _context.PrintSettings.Update(printSettings);
            await _context.SaveChangesAsync(cancellationToken);
            return printSettings;
        }

        public async Task<Branch?> GetBranchByIdAsync(int branchId, CancellationToken cancellationToken = default)
        {
            return await _context.Branches.FindAsync(new object[] { branchId }, cancellationToken);
        }

        public async Task<SupplierPayment?> GetSupplierPaymentByIdAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            return await _context.SupplierPayments
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Branch)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Product)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.BookingUnit)
                        .ThenInclude(bu => bu!.BaseUnit)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.BookingUnit)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        }
    }
}