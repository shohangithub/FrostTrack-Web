namespace Application.RequestDTO;

public record BillCollectionRequest(
    string TransactionCode,
    DateTime TransactionDate,
    int BranchId,
    Guid BookingId,
    decimal Amount,
    string PaymentMethod,
    string? PaymentReference,
    string? Note
);
