namespace Domain.Entitites;

[Table("SupplierPaymentDetails", Schema = "payment")]
public class SupplierPaymentDetail : AuditableEntity<long>
{
    public long SupplierPaymentId { get; set; }
    public required SupplierPayment SupplierPayment { get; set; }
    public long? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public long? SalesId { get; set; }
    public Sales? Sales { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal InvoiceAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal PreviousPaidAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal CurrentPaymentAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal RemainingAmount { get; set; }
}