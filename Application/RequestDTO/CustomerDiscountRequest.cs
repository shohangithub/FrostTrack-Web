namespace Application.RequestDTO;

public record CustomerDiscountRequest(
    string TransactionCode,
    DateTime TransactionDate,
    int BranchId,
    int CustomerId,
    decimal DiscountAmount,
    string DiscountReason,
    decimal? TotalDue = null,
    decimal? CurrentDue = null,
    string? Note = null,
    Guid? BookingId = null
);
