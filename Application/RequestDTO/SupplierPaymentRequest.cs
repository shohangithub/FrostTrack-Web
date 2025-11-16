namespace Application.RequestDTO;

public record SupplierPaymentRequest(
    long Id,
    string PaymentNumber,
    DateTime PaymentDate,
    string PaymentType,
    int? SupplierId,
    int? CustomerId,
    string PaymentMethod,
    int? BankId,
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