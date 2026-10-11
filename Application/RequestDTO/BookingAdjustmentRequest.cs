namespace Application.RequestDTO;

/// <summary>
/// Discount &amp; Adjustment page entry.
/// MINUS reduces the due (saved as a BILL_DISCOUNT transaction, with or without a booking);
/// PLUS adds a charge (saved as a MANUAL_ADJUSTMENT charge-ledger row, booking required).
/// </summary>
public record BookingAdjustmentRequest(
    string TransactionCode,
    DateTime TransactionDate,
    int BranchId,
    int CustomerId,
    Guid? BookingId,
    string Direction,
    decimal Amount,
    string Reason,
    string? Note = null
);
