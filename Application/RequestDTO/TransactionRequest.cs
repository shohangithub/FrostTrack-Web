namespace Application.RequestDTO;

public record TransactionRequest(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    Guid TransactionHeadId, // References TransactionHead table
    int BranchId,
    decimal Amount,
    string? Note,
    // Optional fields with defaults
    int? CustomerId = null,
    Guid? BookingId = null,
        Guid? DeliveryId = null,
        int? SupplierId = null,
    decimal DiscountAmount = 0,
    decimal AdjustmentValue = 0,
    decimal NetAmount = 0,
    string? PaymentMethod = null, // Will be set to CASH in service if null
    string? PaymentReference = null,
    string? Category = null,
    string? SubCategory = null,
    string Description = ""
);
