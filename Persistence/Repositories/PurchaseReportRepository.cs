

using Application.RequestDTO;
using Application.Services.Common;
using Mapster;

public class PurchaseReportRepository : IPurchaseReportRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DefaultValueInjector _defaultValueInjector;
    public PurchaseReportRepository(ApplicationDbContext context, DefaultValueInjector defaultValueInjector)
    {
        _context = context;
        _defaultValueInjector = defaultValueInjector;
    }



    public Task<PurchaseInvoiceReportResponse?> GetPurchaseInvoiceAsync(long purchaseId, CancellationToken cancellationToken = default)
    {
        var query = from p in _context.Purchases
                    join pd in _context.PurchaseDetails on p.Id equals pd.PurchaseId
                    join product in _context.Products on pd.ProductId equals product.Id
                    join unitConversion in _context.UnitConversions on pd.PurchaseUnitId equals unitConversion.Id
                    //join c in _context.Companies on p. equals c.Id
                    join s in _context.Suppliers on p.SupplierId equals s.Id into ps
                    from supplier in ps.DefaultIfEmpty()
                    join b in _context.Branches on p.BranchId equals b.Id
                    where p.Id == purchaseId
                    select new
                    {
                        Purchase = p,
                        Product = product,
                        PurchaseDetail = pd,
                        UnitConversion = unitConversion,
                        //Company = c,
                        Supplier = supplier,
                        Branch = b
                    };

        return query.GroupBy(x => new
        {
            x.Purchase.Id,
            x.Purchase.InvoiceAmount,
            x.Purchase.InvoiceDate,
            x.Supplier,
            x.Branch
        })
        .Select(g => new PurchaseInvoiceReportResponse(
            g.Key.Id,
            g.First().Purchase.InvoiceNumber,
            g.Key.InvoiceDate,
            g.Sum(x => x.PurchaseDetail.PurchaseAmount),
            0,
            g.First().Purchase.VatAmount,
            g.First().Purchase.DiscountPercent,
            g.First().Purchase.DiscountAmount,
            g.First().Purchase.OtherCost,
            g.Key.InvoiceAmount,
            g.First().Purchase.PaidAmount,
            g.First().Purchase.BranchId,
            new SupplierReportResponse(
                g.Key.Supplier.SupplierName,
                g.Key.Supplier.SupplierCode,
                g.Key.Supplier.SupplierBarcode,
                g.Key.Supplier.SupplierMobile,
                g.Key.Supplier.Address,
                g.Key.Supplier.CreditLimit,
                g.Key.Supplier.OpeningBalance,
                g.Key.Supplier.PreviousDue
            ),
            g.Select(x => new PurchaseDetailReportResponse(
                x.PurchaseDetail.Id,
                x.PurchaseDetail.PurchaseRate,
                x.PurchaseDetail.PurchaseQuantity,
                x.PurchaseDetail.PurchaseAmount,
                new ProductReportResponse(
                    x.Product.Id,
                    x.Product.ProductName,
                    x.Product.ProductCode,
                    x.Product.CategoryId,
                    x.Product.DefaultUnitId,
                    x.Product.ImageUrl,
                    x.Product.BookingRate,
                    x.Product.IsActive,
                    x.Product.Status
                ),
                new UnitConversionReportResponse(
                    x.UnitConversion.Id,
                    x.UnitConversion.UnitName,
                    x.UnitConversion.BaseUnitId,
                    x.UnitConversion.ConversionValue,
                    x.UnitConversion.Description,
                    x.UnitConversion.IsActive,
                    x.UnitConversion.Status
                )
            )).ToList()
        )).FirstOrDefaultAsync(cancellationToken);
    }
}

