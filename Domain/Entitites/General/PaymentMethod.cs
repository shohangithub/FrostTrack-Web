using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entitites;

[Table("PaymentMethods", Schema = "general")]
public class PaymentMethod : AuditableEntity<int>
{
    public required string MethodName { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; } = string.Empty;
    public required string Category { get; set; } // Cash, Bank, Digital, Card
    public bool RequiresBankAccount { get; set; }
    public bool RequiresCheckDetails { get; set; }
    public bool RequiresOnlineDetails { get; set; }
    public bool RequiresMobileWalletDetails { get; set; }
    public bool RequiresCardDetails { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? IconClass { get; set; } // CSS icon class for UI
    public int? BranchId { get; set; }
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
}