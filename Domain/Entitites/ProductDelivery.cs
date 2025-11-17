namespace Domain.Entitites;

[Table("ProductDeliveries", Schema = "product")]
public class ProductDelivery : AuditableEntity<long>
{
    public required string DeliveryNumber { get; set; }
    public required DateTime DeliveryDate { get; set; }
    public required int CustomerId { get; set; }
    public Customer Customer { get; set; }
    public required int BranchId { get; set; }
    public Branch Branch { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal VatAmount { get; set; }
    public required float DiscountPercent { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal OtherCost { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal PaidAmount { get; set; }
    public string? Notes { get; set; }

    public ICollection<ProductDeliveryDetail> ProductDeliveryDetails { get; set; } = [];
}
