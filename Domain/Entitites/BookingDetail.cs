namespace Domain.Entitites;

[Table("BookingDetail", Schema = "product")]
public class BookingDetail : AuditableEntity<Guid>
{
    public Booking? Booking { get; set; }
    public required int ProductId { get; set; }
    public Product? Product { get; set; }
    public int BookingUnitId { get; set; }
    public UnitConversion? BookingUnit { get; set; }
    public required float BookingQuantity { get; set; }
    public required string BillType { get; set; } = BillTypes.Monthly;
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BookingRate { get; set; }
    // Base Conversion from Receive Unit to Base Unit
    public required decimal BaseQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BaseRate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastDeliveryDate { get; set; }
    public ICollection<DeliveryDetail> DeliveryDetails { get; set; } = [];
}
