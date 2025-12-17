namespace Domain.Entitites;

[Table("Transactions", Schema = "finance")]
public class Transaction : AuditableEntity<Guid>
{
    public required string TransactionCode { get; set; }
    public required DateTime TransactionDate { get; set; }

    // Transaction Head (replaces static TransactionType and TransactionFlow)
    public Guid TransactionHeadId { get; set; }
    public TransactionHead? TransactionHead { get; set; }
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

    // Employee tracking (for salary payments)
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

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

public static class PaymentMethods
{
    public const string CASH = "CASH";
    public const string BANK_TRANSFER = "BANK_TRANSFER";
    public const string CHEQUE = "CHEQUE";
    public const string CARD = "CARD";
    public const string MOBILE_BANKING = "MOBILE_BANKING";
    public const string CREDIT = "CREDIT"; // Pay later
}

public static class TransactionEntityNames
{
    public const string BOOKING = "Booking";
    public const string DELIVERY = "Delivery";
}