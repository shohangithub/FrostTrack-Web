namespace Domain.Entitites;

[Table("TransactionHeads", Schema = "finance")]
public class TransactionHead : AuditableEntity<Guid>
{
    [Required]
    [MaxLength(100)]
    public required string Code { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Type { get; set; } // DEBIT or CREDIT

    [MaxLength(50)]
    public required string DisplayType { get; set; } // INCOME or EXPENSE / IN or OUT  

    [Required]
    [MaxLength(50)]
    public required string UsageFor { get; set; } // BILL_COLLECTION, TRANSACTION, BANK_TRANSACTION, BOOKING, DELEVERY, SALARY
    public int SortOrder { get; set; } = 0;

    [MaxLength(500)]
    public string? Description { get; set; }

    public required bool IsActive { get; set; } = true;

    public bool IsSystem { get; set; }

    [MaxLength(50)]
    public string? ColorCode { get; set; }

    [MaxLength(50)]
    public string? IconClass { get; set; }

    [NotMapped]
    public string Status => IsActive ? "Active" : "Inactive";
}

// Static class for Type constants
public static class TransactionHeadTypes
{
    public const string DEBIT = "DEBIT";   // Expense/Out
    public const string CREDIT = "CREDIT"; // Income/In
}


public static class UsageFor
{
    public const string BILL_COLLECTION = "BILL_COLLECTION"; // Money IN from customer bill payments
    public const string TRANSACTION = "TRANSACTION"; // Generic money IN/OUT
    public const string BANK_TRANSACTION = "BANK_TRANSACTION"; // Money IN/OUT via bank
    public const string BOOKING = "BOOKING"; // Money IN from bookings
    public const string DELEVERY = "DELEVERY"; // Money IN from delevery payments
     public const string SALARY = "SALARY";
    // public const string OFFICE_COST = "OFFICE_COST  "; // Money OUT for office expenses
    // public const string BILL_PAYMENT = "BILL_PAYMENT"; // Money OUT for vendor bills
    // public const string ADJUSTMENT = "ADJUSTMENT"; // IN/OUT adjustments
    // public const string REFUND = "REFUND"; // Money OUT refunds to customers
    // // Money OUT for employee salaries   
    // public const string OTHER = "OTHER"; // Miscellaneous transactions
}
