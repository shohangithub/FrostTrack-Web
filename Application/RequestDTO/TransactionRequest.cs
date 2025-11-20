namespace Application.RequestDTO;

public record TransactionRequest(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    string TransactionType,
    string TransactionFlow,
    string EntityName,
    string EntityId,
    int BranchId,
    int? CustomerId,
    Guid? BookingId,
    decimal Amount,
    decimal DiscountAmount,
    decimal AdjustmentValue,
    decimal NetAmount,
    string PaymentMethod,
    string? PaymentReference,
    string? Category,
    string? SubCategory,
    string Description,
    string? Note,
    string? VendorName,
    string? VendorContact,
    DateTime? BillingPeriodStart,
    DateTime? BillingPeriodEnd,
    string? AttachmentPath
);
