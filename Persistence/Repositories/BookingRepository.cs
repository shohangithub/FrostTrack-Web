using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

namespace Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;

    public BookingRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Include(p => p.BookingDetails)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public IQueryable<Booking> Query()
    {
        return _context.Bookings
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.Product)
            .Include(x => x.BookingDetails)
            .ThenInclude(x => x.BookingUnit)
            .Include(x => x.Customer)
            .Include(x => x.Branch)
            .AsNoTracking();
    }

    // public async Task<BookingResponse> ManageUpdate(BookingRequest request, Booking existingData, CancellationToken cancellationToken = default)
    // {
    //     List<Guid> bookingDetailsIds = new();

    //     foreach (var item in request.BookingDetails)
    //     {
    //         if (item.Id == Guid.Empty)
    //         {
    //             var newDetail = item.Adapt<BookingDetail>();
    //             newDetail.BillType = BillTypes.Monthly; // Set default BillType

    //             // Calculate BaseQuantity and BaseRate from unit conversion
    //             var unitConversion = await _context.UnitConversions
    //                 .FirstOrDefaultAsync(x => x.Id == item.BookingUnitId, cancellationToken);

    //             if (unitConversion != null)
    //             {
    //                 newDetail.BaseQuantity = (decimal)(item.BookingQuantity * unitConversion.ConversionValue);
    //                 newDetail.BaseRate = item.BookingRate / (decimal)unitConversion.ConversionValue;
    //             }
    //             else
    //             {
    //                 newDetail.BaseQuantity = (decimal)item.BookingQuantity;
    //                 newDetail.BaseRate = item.BookingRate;
    //             }

    //             _defaultValueInjector.InjectCreatingAudit<BookingDetail, Guid>(newDetail);

    //             existingData.BookingDetails.Add(newDetail);
    //             _context.Entry(newDetail).State = EntityState.Added;
    //         }
    //         else
    //         {
    //             var eDetails = existingData.BookingDetails.FirstOrDefault(x => x.Id == item.Id);
    //             if (eDetails == null) throw new Exception("Booking item mismatched!");

    //             eDetails.ProductId = item.ProductId;
    //             eDetails.BookingQuantity = item.BookingQuantity;
    //             eDetails.BookingRate = item.BookingRate;
    //             eDetails.BookingUnitId = item.BookingUnitId;

    //             // Calculate BaseQuantity and BaseRate from unit conversion
    //             var unitConversion = await _context.UnitConversions
    //                 .FirstOrDefaultAsync(x => x.Id == item.BookingUnitId, cancellationToken);

    //             if (unitConversion != null)
    //             {
    //                 eDetails.BaseQuantity = (decimal)(item.BookingQuantity * unitConversion.ConversionValue);
    //                 eDetails.BaseRate = item.BookingRate / (decimal)unitConversion.ConversionValue;
    //             }
    //             else
    //             {
    //                 eDetails.BaseQuantity = (decimal)item.BookingQuantity;
    //                 eDetails.BaseRate = item.BookingRate;
    //             }

    //             _defaultValueInjector.InjectUpdatingAudit<BookingDetail, Guid>(eDetails);
    //             _context.Entry(eDetails).State = EntityState.Modified;

    //             bookingDetailsIds.Add(item.Id);
    //         }
    //     }

    //     var deletedItems = existingData.BookingDetails.Where(x => x.Id != Guid.Empty && !bookingDetailsIds.Contains(x.Id));
    //     foreach (var item in deletedItems)
    //     {
    //         _context.Entry(item).State = EntityState.Deleted;
    //     }

    //     _context.Entry(existingData).State = EntityState.Modified;

    //     var result = await _context.SaveChangesAsync(cancellationToken);
    //     var response = existingData.Adapt<BookingResponse>();

    //     return response;
    // }


    public async Task<BookingResponse> ManageUpdate(
        BookingRequest request,
        Booking existingData,
        CancellationToken cancellationToken = default)
    {
        // TRACK request item IDs
        var incomingIds = request.BookingDetails
            .Where(x => x.Id != Guid.Empty)
            .Select(x => x.Id)
            .ToHashSet();

        // PROCESS ADD + UPDATE
        foreach (var item in request.BookingDetails)
        {
            // ------------ ADD NEW ------------
            if (item.Id == Guid.Empty)
            {
                var newDetail = item.Adapt<BookingDetail>();
                newDetail.BillType = BillTypes.Monthly;

                var unitConversion = await _context.UnitConversions
                    .FirstOrDefaultAsync(x => x.Id == item.BookingUnitId, cancellationToken);

                if (unitConversion != null)
                {
                    newDetail.BaseQuantity = (decimal)(item.BookingQuantity * unitConversion.ConversionValue);
                    newDetail.BaseRate = item.BookingRate / (decimal)unitConversion.ConversionValue;
                }
                else
                {
                    newDetail.BaseQuantity = (decimal)item.BookingQuantity;
                    newDetail.BaseRate = item.BookingRate;
                }

                // Calculate LastDeliveryDate based on BillType
                newDetail.LastDeliveryDate = CalculateLastDeliveryDate(existingData.BookingDate, newDetail.BillType);

                _defaultValueInjector.InjectCreatingAudit<BookingDetail, Guid>(newDetail);

                existingData.BookingDetails.Add(newDetail);   // EF auto-adds
                continue;
            }

            // ------------ UPDATE EXISTING ------------
            var eDetails = existingData.BookingDetails.FirstOrDefault(x => x.Id == item.Id);
            if (eDetails == null)
                throw new Exception("Booking item mismatched!");

            eDetails.ProductId = item.ProductId;
            eDetails.BookingQuantity = item.BookingQuantity;
            eDetails.BookingRate = item.BookingRate;
            eDetails.BookingUnitId = item.BookingUnitId;

            var unitConv = await _context.UnitConversions
                .FirstOrDefaultAsync(x => x.Id == item.BookingUnitId, cancellationToken);

            if (unitConv != null)
            {
                eDetails.BaseQuantity = (decimal)(item.BookingQuantity * unitConv.ConversionValue);
                eDetails.BaseRate = item.BookingRate / (decimal)unitConv.ConversionValue;
            }
            else
            {
                eDetails.BaseQuantity = (decimal)item.BookingQuantity;
                eDetails.BaseRate = item.BookingRate;
            }

            // Recalculate LastDeliveryDate based on BillType
            eDetails.LastDeliveryDate = CalculateLastDeliveryDate(existingData.BookingDate, eDetails.BillType);

            _defaultValueInjector.InjectUpdatingAudit<BookingDetail, Guid>(eDetails);
        }

        // ------------ DELETE REMOVED ITEMS ------------
        var toDelete = existingData.BookingDetails
            .Where(x => x.Id != Guid.Empty && !incomingIds.Contains(x.Id))
            .ToList();

        foreach (var d in toDelete)
            existingData.BookingDetails.Remove(d);  // EF auto-deletes

        await _context.SaveChangesAsync(cancellationToken);

        return existingData.Adapt<BookingResponse>();
    }

    private DateTime CalculateLastDeliveryDate(DateTime bookingDate, string billType)
    {
        return billType switch
        {
            BillTypes.Hourly => bookingDate.AddHours(1),
            BillTypes.Daily => bookingDate.AddDays(1),
            BillTypes.Weekly => bookingDate.AddDays(7),
            BillTypes.Monthly => bookingDate.AddMonths(1),
            BillTypes.Yearly => bookingDate.AddYears(1),
            _ => bookingDate.AddMonths(1) // Default to monthly
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existingData = await _context.Bookings
            .Include(x => x.BookingDetails)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingData == null) throw new ArgumentNullException("Booking record not found!");

        foreach (var item in existingData.BookingDetails)
        {
            _context.Entry(item).State = EntityState.Deleted;
        }

        _context.Entry(existingData).State = EntityState.Deleted;

        var result = await _context.SaveChangesAsync(cancellationToken);

        return result > 0;
    }
}
