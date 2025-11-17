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

    public async Task<ProductReceive?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductReceives
            .Include(p => p.ProductReceiveDetails)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public IQueryable<ProductReceive> Query()
    {
        return _context.ProductReceives.Include(x => x.ProductReceiveDetails).AsNoTracking();
    }

    public async Task<ProductReceiveResponse> ManageUpdate(ProductReceiveRequest request, ProductReceive existingData, CancellationToken cancellationToken = default)
    {
        var quantityDictionary = new List<ProductReceiveDictionary>();
        List<long> receiveDetailsIds = new();

        foreach (var item in request.ProductReceiveDetails)
        {
            if (item.Id == 0)
            {
                var newDetail = item.Adapt<ProductReceiveDetail>();
                newDetail.ProductReceiveId = request.Id;
                _defaultValueInjector.InjectCreatingAudit<ProductReceiveDetail, long>(newDetail);

                quantityDictionary.Add(new ProductReceiveDictionary(item.ProductId, item.ReceiveQuantity, item.BookingRate, item.ReceiveUnitId));

                existingData.ProductReceiveDetails.Add(newDetail);
                _context.Entry(newDetail).State = EntityState.Added;
            }
            else
            {
                var eDetails = existingData.ProductReceiveDetails.FirstOrDefault(x => x.Id == item.Id);
                if (eDetails == null) throw new Exception("Receive item mismatched!");

                if (item.ReceiveQuantity != eDetails.ReceiveQuantity
                    || item.ReceiveAmount != eDetails.ReceiveAmount
                    || item.ReceiveUnitId != eDetails.ReceiveUnitId)
                {
                    quantityDictionary.Add(new ProductReceiveDictionary(item.ProductId, item.ReceiveQuantity - eDetails.ReceiveQuantity, item.BookingRate, item.ReceiveUnitId));

                    eDetails.ReceiveAmount = item.ReceiveAmount;
                    eDetails.ReceiveQuantity = item.ReceiveQuantity;
                    eDetails.BookingRate = item.BookingRate;
                    eDetails.ReceiveUnitId = item.ReceiveUnitId;

                    _defaultValueInjector.InjectUpdatingAudit<ProductReceiveDetail, long>(eDetails);
                    _context.Entry(eDetails).State = EntityState.Modified;
                }
                else
                {
                    quantityDictionary.Add(new ProductReceiveDictionary(item.ProductId, item.ReceiveQuantity - eDetails.ReceiveQuantity, item.BookingRate, item.ReceiveUnitId));
                    _context.Entry(eDetails).State = EntityState.Unchanged;
                }

                receiveDetailsIds.Add(item.Id);
            }
        }

        var deletedItems = existingData.ProductReceiveDetails.Where(x => x.Id != 0 && !receiveDetailsIds.Contains(x.Id));
        foreach (var item in deletedItems)
        {
            quantityDictionary.Add(new ProductReceiveDictionary(item.ProductId, -item.ReceiveQuantity, item.BookingRate, item.ReceiveUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions
            .Where(x => existingData.ProductReceiveDetails.Select(x => x.ReceiveUnitId).Contains(x.Id))
            .ToListAsync(cancellationToken);

        var existingStocks = await _context.Stocks
            .Where(x => existingData.ProductReceiveDetails.Select(x => x.ProductId).Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");

            if (requestStock.Quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.ReceiveUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.Quantity * conversionUnit.ConversionValue);

                stock.StockQuantity += baseQuantity;
                stock.LastPurchaseRate = requestStock.BookingRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }

        var newStocks = request.ProductReceiveDetails
            .Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId))
            .ToList();

        foreach (var stock in newStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");

            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.ReceiveUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.Quantity * conversionUnit.ConversionValue);

            var nStock = new Stock
            {
                BranchId = request.BranchId,
                ProductId = stock.ProductId,
                UnitConversionId = conversionUnit.Id,
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

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var existingData = await _context.ProductReceives
            .Include(x => x.ProductReceiveDetails)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (existingData == null) throw new ArgumentNullException("Receive record not found!");

        var quantityDictionary = new List<ProductReceiveDictionary>();
        foreach (var item in existingData.ProductReceiveDetails)
        {
            quantityDictionary.Add(new ProductReceiveDictionary(item.ProductId, -item.ReceiveQuantity, item.BookingRate, item.ReceiveUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions
            .Where(x => quantityDictionary.Select(x => x.ReceiveUnitId).Contains(x.Id))
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
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.ReceiveUnitId);
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

public record ProductReceiveDictionary(int ProductId, float Quantity, decimal BookingRate, int ReceiveUnitId);
