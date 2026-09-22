namespace Application.RequestDTO;

public record DeliveryBillCollectionRequest(
    string TransactionCode,
    DateTime TransactionDate,
    int BranchId,
    List<Guid> DeliveryIds,
    decimal Amount,
    string PaymentMethod,
    string? PaymentReference,
    string? Note,
    int? BankId = null,
    int? CustomerId = null,
    Guid? BookingId = null,
    decimal AdvanceAmount = 0
);
