using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

namespace Persistence.Repositories;

public class ProductReceiveRepository : IProductReceiveRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;

    public ProductReceiveRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector)
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
        return _context.Bookings.Include(x => x.BookingDetails).AsNoTracking();
    }

    public async Task<ProductReceiveResponse> ManageUpdate(ProductReceiveRequest request, Booking existingData, CancellationToken cancellationToken = default)
    {
        var quantityDictionary = new List<BookingDictionary>();
        List<Guid> bookingDetailsIds = new();

        foreach (var item in request.BookingDetails)
        {
            if (item.Id == Guid.Empty)
            {
                var newDetail = item.Adapt<BookingDetail>();
                _defaultValueInjector.InjectCreatingAudit<BookingDetail, Guid>(newDetail);

                quantityDictionary.Add(new BookingDictionary(item.ProductId, item.BookingQuantity, item.BookingRate, item.BookingUnitId));

                existingData.BookingDetails.Add(newDetail);
                _context.Entry(newDetail).State = EntityState.Added;
            }
            else
            {
                var eDetails = existingData.BookingDetails.FirstOrDefault(x => x.Id == item.Id);
                if (eDetails == null) throw new Exception("Booking item mismatched!");

                if (item.BookingQuantity != eDetails.BookingQuantity
                    || item.BookingUnitId != eDetails.BookingUnitId)
                {
                    quantityDictionary.Add(new BookingDictionary(item.ProductId, item.BookingQuantity - eDetails.BookingQuantity, item.BookingRate, item.BookingUnitId));

                    eDetails.BookingQuantity = item.BookingQuantity;
                    eDetails.BookingRate = item.BookingRate;
                    eDetails.BookingUnitId = item.BookingUnitId;
                    eDetails.BaseQuantity = item.BaseQuantity;
                    eDetails.BaseRate = item.BaseRate;

                    _defaultValueInjector.InjectUpdatingAudit<BookingDetail, Guid>(eDetails);
                    _context.Entry(eDetails).State = EntityState.Modified;
                }
                else
                {
                    quantityDictionary.Add(new BookingDictionary(item.ProductId, item.BookingQuantity - eDetails.BookingQuantity, item.BookingRate, item.BookingUnitId));
                    _context.Entry(eDetails).State = EntityState.Unchanged;
                }

                bookingDetailsIds.Add(item.Id);
            }
        }

        var deletedItems = existingData.BookingDetails.Where(x => x.Id != Guid.Empty && !bookingDetailsIds.Contains(x.Id));
        foreach (var item in deletedItems)
        {
            quantityDictionary.Add(new BookingDictionary(item.ProductId, -item.BookingQuantity, item.BookingRate, item.BookingUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions
            .Where(x => existingData.BookingDetails.Select(x => x.BookingUnitId).Contains(x.Id))
            .ToListAsync(cancellationToken);

        var existingStocks = await _context.Stocks
            .Where(x => existingData.BookingDetails.Select(x => x.ProductId).Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");

            if (requestStock.Quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.BookingUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.Quantity * conversionUnit.ConversionValue);

                stock.StockQuantity += baseQuantity;
                stock.LastPurchaseRate = requestStock.BookingRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }

        var newStocks = request.BookingDetails
            .Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId))
            .ToList();

        foreach (var stock in newStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");

            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.BookingUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.Quantity * conversionUnit.ConversionValue);

            var nStock = new Stock
            {
                BranchId = request.BranchId,
                ProductId = stock.ProductId,
                UnitId = conversionUnit.Id,
                StockQuantity = baseQuantity,
                LastPurchaseRate = requestStock.BookingRate,
            };
            _defaultValueInjector.InjectCreatingAudit<Stock, long>(nStock);
            _context.Entry(nStock).State = EntityState.Added;
        }

        #endregion

        _context.Entry(existingData).State = EntityState.Modified;

        var result = await _context.SaveChangesAsync(cancellationToken);
        var response = existingData.Adapt<ProductReceiveResponse>();

        return response;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existingData = await _context.Bookings
            .Include(x => x.BookingDetails)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (existingData == null) throw new ArgumentNullException("Booking record not found!");

        var quantityDictionary = new List<BookingDictionary>();
        foreach (var item in existingData.BookingDetails)
        {
            quantityDictionary.Add(new BookingDictionary(item.ProductId, -item.BookingQuantity, item.BookingRate, item.BookingUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions
            .Where(x => quantityDictionary.Select(x => x.BookingUnitId).Contains(x.Id))
            .ToListAsync();

        var existingStocks = await _context.Stocks
            .Where(x => quantityDictionary.Select(x => x.ProductId).Contains(x.ProductId))
            .ToListAsync();

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");

            if (requestStock.Quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.BookingUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.Quantity * conversionUnit.ConversionValue);

                stock.StockQuantity += baseQuantity;
                stock.LastPurchaseRate = requestStock.BookingRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }

        #endregion

        _context.Entry(existingData).State = EntityState.Deleted;

        var result = await _context.SaveChangesAsync();

        return result > 0;
    }
}

public record BookingDictionary(int ProductId, float Quantity, decimal BookingRate, int BookingUnitId);
