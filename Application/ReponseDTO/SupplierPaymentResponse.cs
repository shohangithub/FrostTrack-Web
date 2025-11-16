using Domain.Entitites;

namespace Application.ReponseDTO;

public record SupplierPaymentResponse(
    long Id,
    string PaymentNumber,
    DateTime PaymentDate,
    string PaymentType,
    int? SupplierId,
    Supplier? Supplier,
    int? CustomerId,
    Customer? Customer,
    string PaymentMethod,
    int? BankId,
    Bank? Bank,
    string? CheckNumber,
    DateTime? CheckDate,
    // Online payment fields
    string? OnlinePaymentMethod,
    string? TransactionId,
    string? GatewayReference,
    // Mobile wallet fields
    string? MobileWalletType,
    string? WalletNumber,
    string? WalletTransactionId,
    // Card payment fields
    string? CardType,
    string? CardLastFour,
    string? CardTransactionId,
    decimal PaymentAmount,
    string? Notes,
    int BranchId
);

public record SupplierPaymentListResponse(
    long Id,
    string PaymentNumber,
    DateTime PaymentDate,
    string PaymentType,
    int? SupplierId,
    Supplier? Supplier,
    int? CustomerId,
    Customer? Customer,
    string PaymentMethod,
    int? BankId,
    Bank? Bank,
    string? CheckNumber,
    DateTime? CheckDate,
    decimal PaymentAmount,
    string? Notes,
    int BranchId,
    Branch Branch
);