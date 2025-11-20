namespace Domain.Entitites;

[Table("Transactions", Schema = "finance")]
public class Transaction : AuditableEntity<Guid>
{
    public required string TransactionCode { get; set; }
    public required DateTime TransactionDate { get; set; }

    public required string TransactionType { get; set; } // BILL_COLLECTION, OFFICE_COST, BILL_PAYMENT
    public required string TransactionFlow { get; set; } // IN or OUT

    // Polymorphic reference to source entity
    public required string EntityName { get; set; } // "Booking", "Vendor", "Expense", etc.
    public required string EntityId { get; set; } // Changed to string to support Guid

    // Branch tracking
    public required int BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Customer tracking (for bill collections)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Booking tracking (for bill collections from booking)
    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    // Financial fields
    [Column(TypeName = "decimal(10, 2)")]
    public required decimal Amount { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal AdjustmentValue { get; set; } = 0;

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal NetAmount { get; set; } // Amount - Discount + Adjustment

    // Payment details
    public required string PaymentMethod { get; set; } // CASH, BANK_TRANSFER, CHEQUE, etc.
    public string? PaymentReference { get; set; } // Cheque/Transaction number

    // Categorization
    public string? Category { get; set; } // ELECTRIC_BILL, SALARY, RENT, etc.
    public string? SubCategory { get; set; } // Additional classification

    // Description and notes
    public required string Description { get; set; }
    public string? Note { get; set; }

    // Vendor/Payee info (for expenses and bill payments)
    public string? VendorName { get; set; }
    public string? VendorContact { get; set; }

    // Billing period (for recurring bills)
    public DateTime? BillingPeriodStart { get; set; }
    public DateTime? BillingPeriodEnd { get; set; }

    // Attachment support
    public string? AttachmentPath { get; set; }

    // Soft delete and archive
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedById { get; set; }

    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedById { get; set; }
}

public static class TransactionTypes
{
    public const string BILL_COLLECTION = "BILL_COLLECTION"; // Money IN from customers
    public const string OFFICE_COST = "OFFICE_COST"; // Money OUT for office expenses
    public const string BILL_PAYMENT = "BILL_PAYMENT"; // Money OUT for vendor bills
    public const string ADJUSTMENT = "ADJUSTMENT"; // IN/OUT adjustments
    public const string REFUND = "REFUND"; // Money OUT refunds to customers
}

public static class TransactionFlows
{
    public const string IN = "IN";   // Money coming in
    public const string OUT = "OUT"; // Money going out
}

public static class PaymentMethods
{
    public const string CASH = "CASH";
    public const string BANK_TRANSFER = "BANK_TRANSFER";
    public const string CHEQUE = "CHEQUE";
    public const string CARD = "CARD";
    public const string MOBILE_BANKING = "MOBILE_BANKING";
    public const string CREDIT = "CREDIT"; // Pay later
}

public static class ExpenseCategories
{
    // Utility Bills
    public const string ELECTRIC_BILL = "ELECTRIC_BILL";
    public const string WATER_BILL = "WATER_BILL";
    public const string GAS_BILL = "GAS_BILL";
    public const string INTERNET_BILL = "INTERNET_BILL";
    public const string PHONE_BILL = "PHONE_BILL";

    // Operational Expenses
    public const string RENT = "RENT";
    public const string SALARY = "SALARY";
    public const string MAINTENANCE = "MAINTENANCE";
    public const string OFFICE_SUPPLIES = "OFFICE_SUPPLIES";
    public const string TRANSPORTATION = "TRANSPORTATION";
    public const string CLEANING = "CLEANING";
    public const string SECURITY = "SECURITY";

    // Business Expenses
    public const string MARKETING = "MARKETING";
    public const string PROFESSIONAL_FEES = "PROFESSIONAL_FEES";
    public const string INSURANCE = "INSURANCE";
    public const string LICENSE_FEES = "LICENSE_FEES";
    public const string TAX = "TAX";

    // Other
    public const string OTHER = "OTHER";
}