namespace Application.RequestDTO;

/// <summary>
/// Payment against one booking: cash/bank amount, customer advance to apply and/or a discount.
/// Their total may not exceed the booking's current due.
/// </summary>
public record BookingPaymentRequest(
    string TransactionCode,
    DateTime TransactionDate,
    int BranchId,
    int CustomerId,
    Guid BookingId,
    decimal CashAmount,
    string PaymentMethod,
    string? PaymentReference,
    int? BankId,
    string? Note,
    decimal AdvanceAmount = 0,
    decimal DiscountAmount = 0,
    string? DiscountReason = null
);
