namespace Domain.Entitites;

[Table("Banks")]
public class Bank : AuditableEntity<int>
{
    public required string BankName { get; set; }
    public required string BankCode { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountTitle { get; set; }
    public string? SwiftCode { get; set; }
    public string? RoutingNumber { get; set; }
    public string? IBANNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal OpeningBalance { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal CurrentBalance { get; set; }
    public string? Description { get; set; }
    public bool IsMainAccount { get; set; } = false;
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    // Navigation properties for future financial transactions
    // public ICollection<BankTransaction> BankTransactions { get; set; } = [];
}