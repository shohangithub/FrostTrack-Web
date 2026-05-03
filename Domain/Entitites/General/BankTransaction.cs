namespace Domain.Entitites;

[Table("BankTransactions")]
public class BankTransaction : AuditableEntity<long>
{
    public required string TransactionNumber { get; set; }
    public required DateTime TransactionDate { get; set; }
    public required int BankId { get; set; }
    public required string TransactionType { get; set; } // "Deposit" or "Withdraw"
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    [Column(TypeName = "decimal(10, 2)")]
    public decimal BalanceAfter { get; set; }
    public string? ReceiptNumber { get; set; }
    public required bool IsActive { get; set; } = true;
    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";

    public int? BranchId { get; set; }

    // Navigation properties
    public Bank Bank { get; set; } = null!;
}