namespace Application.ReponseDTO;

public record CustomerDiscountItemResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    int CustomerId,
    string CustomerName,
    string? CustomerMobile,
    string? CustomerAddress,
    decimal TotalDue,
    decimal DiscountAmount,
    decimal CurrentDue,
    string DiscountReason,
    string? Note,
    Guid? BookingId,
    DateTime? CreatedTime = null
);
