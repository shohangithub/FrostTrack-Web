namespace Domain.Entitites;

[Table("BaseUnits", Schema = "lookup")]
public class BaseUnit : AuditableEntity<int>
{
    public required string UnitName { get; set; }
    public string Description { get; set; } = string.Empty;
    public required bool IsActive { get; set; }
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";


    public ICollection<UnitConversion> UnitConversions { get; set; } = [];
}