namespace Application.ReponseDTO;

public record CustomerBalanceSummaryResponse(
    int CustomerId,
    string CustomerName,
    string CustomerMobile,
    decimal OpeningBalance,
    decimal TotalBookingCharges,
    decimal TotalDeliveryCharges,
    decimal TotalRecurringCharges,
    decimal TotalAccrued,
    decimal TotalPaid,
    decimal NetDue,
    int ActiveBookingsCount,
    List<RecentCustomerPaymentDto> RecentPayments
);

public record RecentCustomerPaymentDto(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    decimal Amount,
    string PaymentMethod,
    string? PaymentReference,
    string? Note,
    string? BankName
);
