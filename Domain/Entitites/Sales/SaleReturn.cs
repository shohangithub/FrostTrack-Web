namespace Domain.Entitites;

[Table("SaleReturns", Schema = "product")]
public class SaleReturn : AuditableEntity<long>
{
    public required string ReturnNumber { get; set; }
    public required DateTime ReturnDate { get; set; }
    public required long SalesId { get; set; }
    public Sales Sales { get; set; } = null!;
    public required int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public required int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Subtotal { get; set; }
    public required float VatPercent { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal VatAmount { get; set; }
    public required float DiscountPercent { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal OtherCost { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal ReturnAmount { get; set; }
    public required string Reason { get; set; }

    public ICollection<SaleReturnDetail> SaleReturnDetails { get; set; } = [];
}