namespace Domain.Entitites;

[Table("Customers")]
public class Customer : AuditableEntity<int>
{
    public required string CustomerName { get; set; }
    public required string CustomerCode { get; set; }
    public string CustomerBarcode { get; set; } = string.Empty;
    public required ECustomerType CustomerType { get; set; }
    public string? CustomerMobile { get; set; }
    public string? CustomerEmail { get; set; }
    public string? OfficePhone { get; set; }
    public string? Address { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal CreditLimit { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal OpeningBalance { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal PreviousDue { get; set; } = decimal.Zero;
    public string? ImageUrl { get; set; } = string.Empty;
    public bool IsSystemDefault { get; set; } = false;
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    public ICollection<Sales> Sales { get; set; } = [];
}
