using Domain.Entitites;

namespace Application.ReponseDTO;

public record TransactionResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    string TransactionType,
    string TransactionFlow,
    string EntityName,
    string EntityId,
    int BranchId,
    Branch? Branch,
    int? CustomerId,
    Customer? Customer,
    Guid? BookingId,
    Booking? Booking,
    decimal Amount,
    decimal DiscountAmount,
    decimal AdjustmentValue,
    decimal NetAmount,
    string PaymentMethod,
    string? PaymentReference,
    string? Category,
    string? SubCategory,
    string Description,
    string? Note,
    string? VendorName,
    string? VendorContact,
    DateTime? BillingPeriodStart,
    DateTime? BillingPeriodEnd,
    string? AttachmentPath
);

public record TransactionListResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    string TransactionType,
    string TransactionFlow,
    int BranchId,
    string BranchName,
    int? CustomerId,
    string? CustomerName,
    decimal NetAmount,
    string PaymentMethod,
    string? Category,
    string Description,
    string? VendorName
);

public record TransactionSummaryResponse(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetCashFlow,
    int TotalTransactions,
    Dictionary<string, decimal> IncomeByType,
    Dictionary<string, decimal> ExpenseByCategory
);

public record CashFlowResponse(
    DateTime Date,
    decimal TotalIn,
    decimal TotalOut,
    decimal NetCashFlow
);
