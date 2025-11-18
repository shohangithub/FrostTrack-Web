namespace Domain.Entitites;

[Table("BookingDetails", Schema = "product")]
public class BookingDetail : AuditableEntity<long>
{
    public long BookingDetailId { get; set; }
    public Booking Booking { get; set; }
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public int BookingUnitId { get; set; }
    public UnitConversion BookingUnit { get; set; }
    public required float BookingQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BookingRate { get; set; }


    // Base Conversion from Receive Unit to Base Unit
    public required decimal BaseQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BaseRate { get; set; }
}
