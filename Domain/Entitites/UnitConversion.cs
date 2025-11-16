namespace Domain.Entitites;

[Table("UnitConversions", Schema = "lookup")]
public class UnitConversion : AuditableEntity<int>
{
    public required string UnitName { get; set; }
    public required int BaseUnitId { get; set; }
    public required BaseUnit BaseUnit { get; set; }
    public required float ConversionValue { get; set; }

    public string Description { get; set; } = string.Empty;
    public required bool IsActive { get; set; }
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
}