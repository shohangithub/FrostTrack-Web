namespace Domain.Entitites;

[Table("Transactions")]
public class Transaction : AuditableEntity<Guid>
{
    public Guid TransactionId { get; set; }
    public required string TransactionCode { get; set; }
    public required string EntityName { get; set; }
    public required int EntityId { get; set; }
    public required string TransactionType { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Amount { get; set; }
    public string? Note { get; set; } = null;


    public Booking? Booking { get; set; } = null;
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public int BookingUnitId { get; set; }
    public UnitConversion BookingUnit { get; set; }
    public required float BookingQuantity { get; set; }



    // Base Conversion from Receive Unit to Base Unit
    public required decimal BaseQuantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal BaseRate { get; set; }
}

public static class TransactionType
{
    public const string ADVANCE_COLLECTION = "ADVANCE_COLLECTION"; // '+'
    public const string BILL_COLLECTION = "BILL_COLLECTION"; // '+'
    public const string DUE_COLLECTION = "DUE_COLLECTION"; // '+'
    public const string BILL_PAYMENT = "BILL_PAYMENT"; // '-'
}