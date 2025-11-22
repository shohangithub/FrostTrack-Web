namespace Application.ReponseDTO;

public record BookingWithDueResponse(
    Guid BookingId,
    string BookingNumber,
    DateTime BookingDate,
    int CustomerId,
    string CustomerName,
    DateTime? LastDeliveryDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);
