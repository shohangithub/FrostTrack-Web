

using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

public class SalesRepository : ISalesRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;
    public SalesRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<Sales> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .Include(p => p.SalesDetails)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public IQueryable<Sales> Query()
    {
        return _context.Sales.Include(x => x.SalesDetails).AsNoTracking();
    }

    public async Task<SalesResponse> ManageUpdate(SalesRequest request, Sales existingData, CancellationToken cancellationToken = default)
    {

        var quantityDictionary = new List<SalesDictionary>();

        List<long> salesDetailsIds = new();
        foreach (var item in request.SalesDetails)
        {
            if (item.Id == 0)
            {
                var newPDetail = item.Adapt<SalesDetail>();
                newPDetail.SalesId = request.Id;
                _defaultValueInjector.InjectCreatingAudit<SalesDetail, long>(newPDetail);

                quantityDictionary.Add(new SalesDictionary(item.ProductId, item.SalesQuantity, item.SalesRate, item.SalesUnitId));

                existingData.SalesDetails.Add(newPDetail);
                _context.Entry(newPDetail).State = EntityState.Added;
            }
            else
            {
                var eDetails = existingData.SalesDetails.FirstOrDefault(x => x.Id == item.Id);
                if (eDetails == null) throw new Exception("Invoice item mismatched !");


                if (item.SalesQuantity != eDetails.SalesQuantity
                || item.SalesAmount != eDetails.SalesAmount
                || item.SalesUnitId != eDetails.SalesUnitId
                )
                {
                    quantityDictionary.Add(new SalesDictionary(item.ProductId, item.SalesQuantity - eDetails.SalesQuantity, item.SalesRate, item.SalesUnitId));

                    eDetails.SalesAmount = item.SalesAmount;
                    eDetails.SalesQuantity = item.SalesQuantity;
                    eDetails.SalesRate = item.SalesRate;
                    eDetails.SalesUnitId = item.SalesUnitId;


                    _defaultValueInjector.InjectUpdatingAudit<SalesDetail, long>(eDetails);
                    _context.Entry(eDetails).State = EntityState.Modified;
                }
                else
                {
                    quantityDictionary.Add(new SalesDictionary(item.ProductId, item.SalesQuantity - eDetails.SalesQuantity, item.SalesRate, item.SalesUnitId));
                    _context.Entry(eDetails).State = EntityState.Unchanged;
                }

                salesDetailsIds.Add(item.Id);
            }
        }

        var deletedItems = existingData.SalesDetails.Where(x => x.Id != 0 && !salesDetailsIds.Contains(x.Id));
        foreach (var item in deletedItems)
        {
            quantityDictionary.Add(new SalesDictionary(item.ProductId, -item.SalesQuantity, item.SalesRate, item.SalesUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions.Where(x => existingData.SalesDetails.Select(x => x.SalesUnitId).Contains(x.Id)).ToListAsync();
        var existingStocks = await _context.Stocks.Where(x => existingData.SalesDetails.Select(x => x.ProductId).Contains(x.ProductId)).ToListAsync();

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.productId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");
            if (requestStock.quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.salesUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.quantity * conversionUnit.ConversionValue);
                //var salesRate = requestStock.SalesAmount / (decimal)baseQuantity;


                stock.StockQuantity -= baseQuantity;
                stock.LastPurchaseRate = requestStock.salesRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }

        //var newStocks = request.SalesDetails.Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId)).ToList();
        //foreach (var stock in newStocks)
        //{

        //    var requestStock = quantityDictionary.FirstOrDefault(x => x.productId == stock.ProductId);
        //    if (requestStock == null) throw new Exception("Internal Server Error");

        //    var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.salesUnitId);
        //    if (conversionUnit == null) throw new Exception("Internal Server Error");

        //    var baseQuantity = (requestStock.quantity * conversionUnit.ConversionValue);

        //    var nStock = new Stock
        //    {
        //        BranchId = request.BranchId,
        //        ProductId = stock.ProductId,
        //        UnitConversionId = conversionUnit.Id,
        //        StockQuantity = baseQuantity,
        //        LastPurchaseRate = requestStock.salesRate,
        //    };
        //    _defaultValueInjector.InjectCreatingAudit<Stock, long>(nStock);
        //    _context.Entry(nStock).State = EntityState.Added;
        //}

        #endregion


        _context.Entry(existingData).State = EntityState.Modified;


        var result = await _context.SaveChangesAsync();
        var response = existingData.Adapt<SalesResponse>();

        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var existingData = await _context.Sales.Include(x => x.SalesDetails).AsNoTracking().Where(x => x.Id == id).FirstOrDefaultAsync();
        if (existingData == null) throw new ArgumentNullException("Invoice not found !");

        var quantityDictionary = new List<SalesDictionary>();
        foreach (var item in existingData.SalesDetails)
        {
            quantityDictionary.Add(new SalesDictionary(item.ProductId, -item.SalesQuantity, item.SalesRate, item.SalesUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions.Where(x => quantityDictionary.Select(x => x.salesUnitId).Contains(x.Id)).ToListAsync();
        var existingStocks = await _context.Stocks.Where(x => quantityDictionary.Select(x => x.productId).Contains(x.ProductId)).ToListAsync();

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.productId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");
            if (requestStock.quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.salesUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.quantity * conversionUnit.ConversionValue);


                stock.StockQuantity += baseQuantity;
                stock.LastPurchaseRate = requestStock.salesRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }


        #endregion


        _context.Entry(existingData).State = EntityState.Deleted;


        var result = await _context.SaveChangesAsync();


        return result > 0; //await _repository.DeleteAsync(existingData, cancellationToken);
    }
}

public record SalesDictionary(int productId, float quantity, decimal salesRate, int salesUnitId);