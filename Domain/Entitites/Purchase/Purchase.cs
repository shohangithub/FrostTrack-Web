namespace Domain.Entitites;

[Table("Purchases", Schema = "product")]
public class Purchase : AuditableEntity<long>
{
    public required string InvoiceNumber { get; set; }
    public required DateTime InvoiceDate { get; set; }
    public required int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
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
    public required decimal InvoiceAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal PaidAmount { get; set; }
    public required int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = [];
}
