namespace Domain.Entitites;

[Table("Damages", Schema = "product")]
public class Damage : AuditableEntity<int>
{
    public required string DamageNumber { get; set; }
    public required DateTime DamageDate { get; set; }
    public required int ProductId { get; set; }
    public Product Product { get; set; }
    public required int UnitId { get; set; }
    public UnitConversion Unit { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Quantity { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal UnitCost { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal TotalCost { get; set; }
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }
}