namespace Domain.Entitites;

[Table("ProductCategories", Schema = "product")]
public class ProductCategory : AuditableEntity<int>
{
    public required string CategoryName { get; set; }
    public string? Description { get; set; } = string.Empty;
    public required bool IsActive { get; set; }
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";


    public ICollection<Product> Products { get; set; } = [];
}