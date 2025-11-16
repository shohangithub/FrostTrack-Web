using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

public class SaleReturnRepository : ISaleReturnRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;
    public SaleReturnRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
    }

    public async Task<SaleReturn> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.SaleReturns
            .Include(p => p.SaleReturnDetails)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken) ?? throw new Exception("Sale Return not found");
    }

    public IQueryable<SaleReturn> Query()
    {
        return _context.SaleReturns.Include(x => x.SaleReturnDetails).AsNoTracking();
    }

    public async Task<SaleReturnResponse> ManageUpdate(SaleReturnRequest request, SaleReturn existingData, CancellationToken cancellationToken = default)
    {
        var quantityDictionary = new List<SaleReturnDictionary>();

        List<long> saleReturnDetailsIds = new();
        foreach (var item in request.SaleReturnDetails)
        {
            if (item.Id == 0)
            {
                var newReturnDetail = item.Adapt<SaleReturnDetail>();
                newReturnDetail.SaleReturnId = request.Id;
                // Audit fields will be automatically set by DbContext.SaveChangesAsync

                quantityDictionary.Add(new SaleReturnDictionary(item.ProductId, item.ReturnQuantity, item.ReturnRate, item.ReturnUnitId));

                existingData.SaleReturnDetails.Add(newReturnDetail);
                _context.Entry(newReturnDetail).State = EntityState.Added;
            }
            else
            {
                var eDetails = existingData.SaleReturnDetails.FirstOrDefault(x => x.Id == item.Id);
                if (eDetails != null)
                {
                    var oldQuantity = eDetails.ReturnQuantity;
                    var oldRate = eDetails.ReturnRate;
                    var oldUnitId = eDetails.ReturnUnitId;

                    eDetails.ProductId = item.ProductId;
                    eDetails.ReturnUnitId = item.ReturnUnitId;
                    eDetails.ReturnRate = item.ReturnRate;
                    eDetails.ReturnQuantity = (decimal)item.ReturnQuantity;
                    eDetails.ReturnAmount = item.ReturnAmount;
                    eDetails.Reason = item.Reason;

                    // Audit fields will be automatically set by DbContext.SaveChangesAsync

                    quantityDictionary.Add(new SaleReturnDictionary(item.ProductId, item.ReturnQuantity, item.ReturnRate, item.ReturnUnitId, (float)oldQuantity, oldRate, oldUnitId));

                    _context.Entry(eDetails).State = EntityState.Modified;
                }

                saleReturnDetailsIds.Add(item.Id);
            }
        }

        var removeList = existingData.SaleReturnDetails.Where(x => !saleReturnDetailsIds.Contains(x.Id)).ToList();
        foreach (var item in removeList)
        {
            quantityDictionary.Add(new SaleReturnDictionary(item.ProductId, 0, item.ReturnRate, item.ReturnUnitId, (float)item.ReturnQuantity, item.ReturnRate, item.ReturnUnitId));
            _context.Entry(item).State = EntityState.Deleted;
        }

        _context.Entry(existingData).State = EntityState.Modified;

        var success = await _context.SaveChangesAsync(cancellationToken);
        if (success > 0)
        {
            await ManageReturnStock(quantityDictionary, cancellationToken);
        }

        var response = existingData.Adapt<SaleReturnResponse>();
        return response;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var existingData = await _context.SaleReturns.Include(x => x.SaleReturnDetails).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existingData == null) return false;

        _context.SaleReturns.Remove(existingData);
        var result = await _context.SaveChangesAsync(cancellationToken);
        return result > 0;
    }

    private async Task ManageReturnStock(List<SaleReturnDictionary> quantityDictionaries, CancellationToken cancellationToken = default)
    {
        foreach (var item in quantityDictionaries)
        {
            var stock = await _context.Stocks.FirstOrDefaultAsync(x => x.ProductId == item.ProductId && x.UnitConversionId == item.UnitId, cancellationToken);
            if (stock != null)
            {
                // For returns, we need to add back to stock
                var newQuantity = item.NewQuantity - item.OldQuantity;
                stock.StockQuantity += newQuantity;

                var newRate = item.NewRate;
                var oldRate = item.OldRate;

                if (newRate != oldRate && newQuantity != 0)
                {
                    stock.LastPurchaseRate = newRate;
                }

                _context.Entry(stock).State = EntityState.Modified;
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record SaleReturnDictionary(int ProductId, float NewQuantity, decimal NewRate, int UnitId, float OldQuantity = 0, decimal OldRate = 0, int OldUnitId = 0);