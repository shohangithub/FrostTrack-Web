namespace Domain.Entitites;

[Table("Organizations")]
public class Organization : BaseEntity<int>
{
    public required string Name { get; set; }
    public string? RegistrationEmail { get; set; } = string.Empty;
    public string? RegistrationPhone { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Package { get; set; }
    public DateTime ValidityDate { get; set; }
    public bool IsSingleBranch { get; set; }
    public Guid TenantKey { get; set; } = Guid.NewGuid();
    public ICollection<Company> Companies { get; set; } = [];
}