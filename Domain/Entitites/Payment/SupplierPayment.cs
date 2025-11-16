namespace Domain.Entitites;

[Table("SupplierPayments", Schema = "payment")]
public class SupplierPayment : AuditableEntity<long>
{
    public required string PaymentNumber { get; set; }
    public required DateTime PaymentDate { get; set; }
    public required string PaymentType { get; set; } // "Supplier" or "Customer"
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public required string PaymentMethod { get; set; } // Cash, Bank, Check, Online, MobileWallet, Card
    public int? BankId { get; set; }
    public Bank? Bank { get; set; }
    public string? CheckNumber { get; set; }
    public DateTime? CheckDate { get; set; }

    // Online payment fields
    public string? OnlinePaymentMethod { get; set; } // PayPal, Stripe, Razorpay, etc.
    public string? TransactionId { get; set; }
    public string? GatewayReference { get; set; }

    // Mobile wallet fields
    public string? MobileWalletType { get; set; } // bKash, Nagad, Google Pay, etc.
    public string? WalletNumber { get; set; }
    public string? WalletTransactionId { get; set; }

    // Card payment fields
    public string? CardType { get; set; } // Visa, MasterCard, American Express, etc.
    public string? CardLastFour { get; set; }
    public string? CardTransactionId { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public required decimal PaymentAmount { get; set; }
    public string? Notes { get; set; }
    public required int BranchId { get; set; }
    public required Branch Branch { get; set; }
    public ICollection<SupplierPaymentDetail> SupplierPaymentDetails { get; set; } = [];
}