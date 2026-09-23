namespace Application.RequestDTO;

public record CustomerPaymentRequest(
    string TransactionCode,
    DateTime TransactionDate,
    int BranchId,
    int CustomerId,
    decimal Amount,
    string PaymentMethod,
    string? PaymentReference = null,
    string? Note = null,
    int? BankId = null,
    Guid? BookingId = null
);
