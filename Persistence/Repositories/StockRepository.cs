using Application.Repositories;

namespace Persistence.Repositories;

public class StockRepository(ApplicationDbContext _context) : IStockRepository
{
    public async Task<bool> ManageAddPurchaseStock(Purchase request, CancellationToken cancellationToken)
    {

        List<int> productIds = request.PurchaseDetails.Select(x => x.ProductId).ToList() ?? new();
        List<int> unitIds = request.PurchaseDetails.Select(x => x.PurchaseUnitId).ToList() ?? new();
        var unitConversions = await _context.UnitConversions.Where(x => unitIds.Contains(x.Id)).ToListAsync();

        var existingStocks = await _context.Stocks.Where(x => productIds.Contains(x.ProductId)).ToListAsync();
        foreach (var stock in existingStocks)
        {
            var requestStock = request.PurchaseDetails.FirstOrDefault(x => productIds.Contains(x.ProductId));
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.PurchaseUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.PurchaseQuantity * conversionUnit.ConversionValue);
            var purchaseRate = requestStock.PurchaseAmount / (decimal)baseQuantity;
            requestStock.PurchaseRate = purchaseRate;

            stock.StockQuantity += baseQuantity;
            stock.LastPurchaseRate = purchaseRate;
            _context.Entry(stock).State = EntityState.Modified;
        }

        var newStocks = request.PurchaseDetails.Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId)).ToList();
        foreach (var stock in newStocks)
        {

            var requestStock = request.PurchaseDetails.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.PurchaseUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");


            var baseQuantity = (requestStock.PurchaseQuantity * conversionUnit.ConversionValue);
            var purchaseRate = requestStock.PurchaseAmount / (decimal)baseQuantity;
            requestStock.PurchaseRate = purchaseRate;

            var nStock = new Stock
            {
                BranchId = request.BranchId,
                CreatedById = request.CreatedById,
                CreatedTime = request.CreatedTime,
                ProductId = stock.ProductId,
                UnitConversionId = conversionUnit.Id,
                StockQuantity = baseQuantity,
                LastPurchaseRate = purchaseRate,
                TenantId = request.TenantId
            };
            _context.Entry(nStock).State = EntityState.Added;
        }

        await _context.Purchases.AddAsync(request);
        var result = await _context.SaveChangesAsync();
        return result > 0;

    }
    public async Task<bool> ManageAddSalesStock(Sales request, CancellationToken cancellationToken)
    {

        List<int> productIds = request.SalesDetails.Select(x => x.ProductId).ToList() ?? new();
        List<int> unitIds = request.SalesDetails.Select(x => x.SalesUnitId).ToList() ?? new();
        var unitConversions = await _context.UnitConversions.Where(x => unitIds.Contains(x.Id)).ToListAsync();

        var existingStocks = await _context.Stocks.Where(x => productIds.Contains(x.ProductId)).ToListAsync();
        foreach (var stock in existingStocks)
        {
            var requestStock = request.SalesDetails.FirstOrDefault(x => productIds.Contains(x.ProductId));
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.SalesUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.SalesQuantity * conversionUnit.ConversionValue);
            var purchaseRate = requestStock.SalesAmount / (decimal)baseQuantity;
            requestStock.SalesRate = purchaseRate;
            ;
            stock.StockQuantity -= baseQuantity;
            stock.LastPurchaseRate = purchaseRate;
            _context.Entry(stock).State = EntityState.Modified;
        }

        //var newStocks = request.SalesDetails.Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId)).ToList();
        //foreach (var stock in newStocks)
        //{

        //    var requestStock = request.SalesDetails.FirstOrDefault(x => x.ProductId == stock.ProductId);
        //    if (requestStock == null) throw new Exception("Internal Server Error");
        //    var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.SalesUnitId);
        //    if (conversionUnit == null) throw new Exception("Internal Server Error");


        //    var baseQuantity = (requestStock.SalesQuantity * conversionUnit.ConversionValue);
        //    var purchaseRate = requestStock.SalesAmount / (decimal)baseQuantity;
        //    requestStock.SalesRate = purchaseRate;

        //    var nStock = new Stock
        //    {
        //        BranchId = request.BranchId,
        //        CreatedById = request.CreatedById,
        //        CreatedTime = request.CreatedTime,
        //        ProductId = stock.ProductId,
        //        UnitConversionId = conversionUnit.Id,
        //        StockQuantity = baseQuantity,
        //        LastPurchaseRate = purchaseRate,
        //        TenantId = request.TenantId
        //    };
        //    _context.Entry(nStock).State = EntityState.Added;
        //}

        await _context.Sales.AddAsync(request);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageAddSaleReturnStock(SaleReturn request, CancellationToken cancellationToken)
    {
        List<int> productIds = request.SaleReturnDetails.Select(x => x.ProductId).ToList() ?? new();
        List<int> unitIds = request.SaleReturnDetails.Select(x => x.ReturnUnitId).ToList() ?? new();
        var unitConversions = await _context.UnitConversions.Where(x => unitIds.Contains(x.Id)).ToListAsync();

        var existingStocks = await _context.Stocks.Where(x => productIds.Contains(x.ProductId)).ToListAsync();
        foreach (var stock in existingStocks)
        {
            var requestStock = request.SaleReturnDetails.FirstOrDefault(x => productIds.Contains(x.ProductId));
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.ReturnUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = ((decimal)requestStock.ReturnQuantity * (decimal)conversionUnit.ConversionValue);
            var returnRate = requestStock.ReturnAmount / (decimal)baseQuantity;
            requestStock.ReturnRate = returnRate;

            // For returns, we add back to stock
            stock.StockQuantity += (float)baseQuantity;
            _context.Entry(stock).State = EntityState.Modified;
        }

        await _context.SaleReturns.AddAsync(request);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageDeleteSaleReturnStock(SaleReturn entity, CancellationToken cancellationToken)
    {
        List<int> productIds = entity.SaleReturnDetails.Select(x => x.ProductId).ToList() ?? new();
        List<int> unitIds = entity.SaleReturnDetails.Select(x => x.ReturnUnitId).ToList() ?? new();
        var unitConversions = await _context.UnitConversions.Where(x => unitIds.Contains(x.Id)).ToListAsync();

        var existingStocks = await _context.Stocks.Where(x => productIds.Contains(x.ProductId)).ToListAsync();
        foreach (var stock in existingStocks)
        {
            var requestStock = entity.SaleReturnDetails.FirstOrDefault(x => productIds.Contains(x.ProductId));
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.ReturnUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = ((decimal)requestStock.ReturnQuantity * (decimal)conversionUnit.ConversionValue);

            // For return deletion, we subtract from stock
            stock.StockQuantity -= (float)baseQuantity;
            _context.Entry(stock).State = EntityState.Modified;
        }

        _context.SaleReturns.Remove(entity);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageBatchDeleteSaleReturnStock(List<SaleReturn> entities, CancellationToken cancellationToken)
    {
        foreach (var entity in entities)
        {
            List<int> productIds = entity.SaleReturnDetails.Select(x => x.ProductId).ToList() ?? new();
            List<int> unitIds = entity.SaleReturnDetails.Select(x => x.ReturnUnitId).ToList() ?? new();
            var unitConversions = await _context.UnitConversions.Where(x => unitIds.Contains(x.Id)).ToListAsync();

            var existingStocks = await _context.Stocks.Where(x => productIds.Contains(x.ProductId)).ToListAsync();
            foreach (var stock in existingStocks)
            {
                var requestStock = entity.SaleReturnDetails.FirstOrDefault(x => productIds.Contains(x.ProductId));
                if (requestStock == null) throw new Exception("Internal Server Error");
                var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.ReturnUnitId);
                if (conversionUnit == null) throw new Exception("Internal Server Error");

                var baseQuantity = ((decimal)requestStock.ReturnQuantity * (decimal)conversionUnit.ConversionValue);

                // For return deletion, we subtract from stock
                stock.StockQuantity -= (float)baseQuantity;
                _context.Entry(stock).State = EntityState.Modified;
            }
        }

        _context.SaleReturns.RemoveRange(entities);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageAddDamageStock(Damage request, CancellationToken cancellationToken)
    {
        var unitConversion = await _context.UnitConversions.FirstOrDefaultAsync(x => x.Id == request.UnitId);
        if (unitConversion == null) throw new Exception("Unit conversion not found");

        var existingStock = await _context.Stocks.FirstOrDefaultAsync(x => x.ProductId == request.ProductId);
        if (existingStock != null)
        {
            var baseQuantity = (float)(request.Quantity * (decimal)unitConversion.ConversionValue);

            // For damage, we subtract from stock (like sales but without revenue)
            existingStock.StockQuantity -= baseQuantity;
            _context.Entry(existingStock).State = EntityState.Modified;
        }

        await _context.Damages.AddAsync(request);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageDeleteDamageStock(Damage entity, CancellationToken cancellationToken)
    {
        var unitConversion = await _context.UnitConversions.FirstOrDefaultAsync(x => x.Id == entity.UnitId);
        if (unitConversion == null) throw new Exception("Unit conversion not found");

        var existingStock = await _context.Stocks.FirstOrDefaultAsync(x => x.ProductId == entity.ProductId);
        if (existingStock != null)
        {
            var baseQuantity = (float)(entity.Quantity * (decimal)unitConversion.ConversionValue);

            // For damage deletion, we add back to stock
            existingStock.StockQuantity += baseQuantity;
            _context.Entry(existingStock).State = EntityState.Modified;
        }

        _context.Damages.Remove(entity);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageBatchDeleteDamageStock(List<Damage> entities, CancellationToken cancellationToken)
    {
        foreach (var entity in entities)
        {
            var unitConversion = await _context.UnitConversions.FirstOrDefaultAsync(x => x.Id == entity.UnitId);
            if (unitConversion == null) throw new Exception("Unit conversion not found");

            var existingStock = await _context.Stocks.FirstOrDefaultAsync(x => x.ProductId == entity.ProductId);
            if (existingStock != null)
            {
                var baseQuantity = (float)(entity.Quantity * (decimal)unitConversion.ConversionValue);

                // For damage deletion, we add back to stock
                existingStock.StockQuantity += baseQuantity;
                _context.Entry(existingStock).State = EntityState.Modified;
            }
        }

        _context.Damages.RemoveRange(entities);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    public async Task<bool> ManageAddProductReceiveStock(Booking request, CancellationToken cancellationToken)
    {
        List<int> productIds = request.BookingDetails.Select(x => x.ProductId).ToList() ?? new();
        List<int> unitIds = request.BookingDetails.Select(x => x.BookingUnitId).ToList() ?? new();
        var unitConversions = await _context.UnitConversions.Where(x => unitIds.Contains(x.Id)).ToListAsync();

        var existingStocks = await _context.Stocks.Where(x => productIds.Contains(x.ProductId)).ToListAsync();
        foreach (var stock in existingStocks)
        {
            var requestStock = request.BookingDetails.FirstOrDefault(x => productIds.Contains(x.ProductId));
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.BookingUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.BookingQuantity * conversionUnit.ConversionValue);
            var bookingRate = requestStock.BookingRate;

            stock.StockQuantity += baseQuantity;
            stock.LastPurchaseRate = bookingRate;
            _context.Entry(stock).State = EntityState.Modified;
        }

        var newStocks = request.BookingDetails.Where(x => !existingStocks.Select(x => x.ProductId).Contains(x.ProductId)).ToList();
        foreach (var stock in newStocks)
        {
            var requestStock = request.BookingDetails.FirstOrDefault(x => x.ProductId == stock.ProductId);
            if (requestStock == null) throw new Exception("Internal Server Error");
            var conversionUnit = unitConversions.FirstOrDefault(x => x.Id == requestStock.BookingUnitId);
            if (conversionUnit == null) throw new Exception("Internal Server Error");

            var baseQuantity = (requestStock.BookingQuantity * conversionUnit.ConversionValue);
            var bookingRate = requestStock.BookingRate;

            var nStock = new Stock
            {
                BranchId = request.BranchId,
                CreatedById = request.CreatedById,
                CreatedTime = request.CreatedTime,
                ProductId = stock.ProductId,
                UnitConversionId = conversionUnit.Id,
                StockQuantity = baseQuantity,
                LastPurchaseRate = bookingRate,
                TenantId = request.TenantId
            };
            _context.Entry(nStock).State = EntityState.Added;
        }

        await _context.Bookings.AddAsync(request);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}

