

using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;
    public PurchaseRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<Purchase?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Purchases
            .Include(p => p.PurchaseDetails)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public IQueryable<Purchase> Query()
    {
        return _context.Purchases.Include(x => x.PurchaseDetails).AsNoTracking();
    }

    public async Task<PurchaseResponse> ManageUpdate(PurchaseRequest request, Purchase existingData, CancellationToken cancellationToken = default)
    {

        var quantityDictionary = new List<PurchaseDictionary>();

        List<long> purchaseDetailsIds = new();

        foreach (var item in request.PurchaseDetails)
        {
            if (item.Id == 0)
            {
                var newPDetail = item.Adapt<PurchaseDetail>();
                newPDetail.PurchaseId = request.Id;
                _defaultValueInjector.InjectCreatingAudit<PurchaseDetail, long>(newPDetail);

                quantityDictionary.Add(new PurchaseDictionary(item.ProductId, item.PurchaseQuantity, item.PurchaseRate, item.PurchaseUnitId));

                existingData.PurchaseDetails.Add(newPDetail);
                _context.Entry(newPDetail).State = EntityState.Added;
            }
            else
            {
                var eDetails = existingData.PurchaseDetails.FirstOrDefault(x => x.Id == item.Id);
                if (eDetails == null) throw new Exception("Invoice item mismatched !");


                if (item.PurchaseQuantity != eDetails.PurchaseQuantity
                || item.PurchaseAmount != eDetails.PurchaseAmount
                || item.PurchaseUnitId != eDetails.PurchaseUnitId
                )
                {
                    quantityDictionary.Add(new PurchaseDictionary(item.ProductId, item.PurchaseQuantity - eDetails.PurchaseQuantity, item.PurchaseRate, item.PurchaseUnitId));

                    eDetails.PurchaseAmount = item.PurchaseAmount;
                    eDetails.PurchaseQuantity = item.PurchaseQuantity;
                    eDetails.PurchaseRate = item.PurchaseRate;
                    eDetails.PurchaseUnitId = item.PurchaseUnitId;


                    _defaultValueInjector.InjectUpdatingAudit<PurchaseDetail, long>(eDetails);
                    _context.Entry(eDetails).State = EntityState.Modified;
                }
                else
                {
                    quantityDictionary.Add(new PurchaseDictionary(item.ProductId, item.PurchaseQuantity - eDetails.PurchaseQuantity, item.PurchaseRate, item.PurchaseUnitId));
                    _context.Entry(eDetails).State = EntityState.Unchanged;
                }

                purchaseDetailsIds.Add(item.Id);
            }
        }

        var deletedItems = existingData.PurchaseDetails.Where(x => x.Id != 0 && !purchaseDetailsIds.Contains(x.Id));
        foreach (var item in deletedItems)
        {
            quantityDictionary.Add(new PurchaseDictionary(item.ProductId, -item.PurchaseQuantity, item.PurchaseRate, item.PurchaseUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions.Where(x => existingData.PurchaseDetails.Select(x => x.PurchaseUnitId).Contains(x.Id)).ToListAsync(cancellationToken);
        var existingStocks = await _context.Stocks.Where(x => existingData.PurchaseDetails.Select(x => x.ProductId).Contains(x.ProductId)).ToListAsync(cancellationToken);

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.productId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");
            if (requestStock.quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.purchaseUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.quantity * conversionUnit.ConversionValue);
                //var purchaseRate = requestStock.PurchaseAmount / (decimal)baseQuantity;


                stock.StockQuantity += baseQuantity;
                stock.LastPurchaseRate = requestStock.purchaseRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }

        var newStocks = request.PurchaseDetails.Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId)).ToList();
        foreach (var stock in newStocks)
        {

            var requestStock = quantityDictionary.FirstOrDefault(x => x.productId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");

            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.purchaseUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.quantity * conversionUnit.ConversionValue);

            var nStock = new Stock
            {
                BranchId = request.BranchId,
                ProductId = stock.ProductId,
                UnitId = conversionUnit.Id,
                StockQuantity = baseQuantity,
                LastPurchaseRate = requestStock.purchaseRate,
            };
            _defaultValueInjector.InjectCreatingAudit<Stock, long>(nStock);
            _context.Entry(nStock).State = EntityState.Added;
        }

        #endregion


        _context.Entry(existingData).State = EntityState.Modified;


        var result = await _context.SaveChangesAsync(cancellationToken);
        var response = existingData.Adapt<PurchaseResponse>();

        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var existingData = await _context.Purchases.Include(x => x.PurchaseDetails).AsNoTracking().Where(x => x.Id == id).FirstOrDefaultAsync();
        if (existingData == null) throw new ArgumentNullException("Invoice not found !");

        var quantityDictionary = new List<PurchaseDictionary>();
        foreach (var item in existingData.PurchaseDetails)
        {
            quantityDictionary.Add(new PurchaseDictionary(item.ProductId, -item.PurchaseQuantity, item.PurchaseRate, item.PurchaseUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        #region FOR STOCK OPERATION

        var unitConversions = await _context.UnitConversions.Where(x => quantityDictionary.Select(x => x.purchaseUnitId).Contains(x.Id)).ToListAsync();
        var existingStocks = await _context.Stocks.Where(x => quantityDictionary.Select(x => x.productId).Contains(x.ProductId)).ToListAsync();

        foreach (var stock in existingStocks)
        {
            var requestStock = quantityDictionary.FirstOrDefault(x => x.productId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");
            if (requestStock.quantity != 0)
            {
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.purchaseUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = (requestStock.quantity * conversionUnit.ConversionValue);


                stock.StockQuantity += baseQuantity;
                stock.LastPurchaseRate = requestStock.purchaseRate;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }


        #endregion


        _context.Entry(existingData).State = EntityState.Deleted;


        var result = await _context.SaveChangesAsync();


        return result > 0; //await _repository.DeleteAsync(existingData, cancellationToken);
    }
}


public record PurchaseDictionary(int productId, float quantity, decimal purchaseRate, int purchaseUnitId);

public record QuantityDictionary(int productId, float quantity);