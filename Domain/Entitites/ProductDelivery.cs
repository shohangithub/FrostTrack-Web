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
    public string? Notes { get; set; }

    public ICollection<ProductDeliveryDetail> ProductDeliveryDetails { get; set; } = [];
}
