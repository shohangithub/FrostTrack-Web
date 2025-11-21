namespace Application.RequestDTO;

public record TransactionRequest(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    string TransactionType,
    string TransactionFlow,
    int BranchId,
    decimal Amount,
    string? Note,
    // Optional fields with defaults
    string? EntityName = "GENERAL",
    string? EntityId = "00000000-0000-0000-0000-000000000000",
    int? CustomerId = null,
    Guid? BookingId = null,
    decimal DiscountAmount = 0,
    decimal AdjustmentValue = 0,
    decimal NetAmount = 0,
    string? PaymentMethod = null, // Will be set to CASH in service if null
    string? PaymentReference = null,
    string? Category = null,
    string? SubCategory = null,
    string Description = "",
    string? VendorName = null,
    string? VendorContact = null,
    DateTime? BillingPeriodStart = null,
    DateTime? BillingPeriodEnd = null,
    string? AttachmentPath = null
);
