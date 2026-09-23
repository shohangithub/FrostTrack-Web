namespace Application.ReponseDTO;

public record CustomerPaymentReportItemResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    int CustomerId,
    string CustomerName,
    string? CustomerMobile,
    string? CustomerAddress,
    decimal Amount,
    string PaymentMethod,
    string? BankName,
    string? PaymentReference,
    string? Note,
    DateTime CreatedTime
);
