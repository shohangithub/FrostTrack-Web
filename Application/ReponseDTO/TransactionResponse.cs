using Domain.Entitites;

namespace Application.ReponseDTO;

public record TransactionResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    Guid TransactionHeadId,
    TransactionHeadLookup TransactionHead,
    string EntityName,
    string EntityId,
    int BranchId,
    Branch? Branch,
    int? CustomerId,
    Customer? Customer,
    Guid? BookingId,
    Booking? Booking,
    int? EmployeeId,
    string? EmployeeName,
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
    string? AttachmentPath,
    decimal? RelatedLabourCharge = null
);

public record TransactionListResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    Guid TransactionHeadId,
    TransactionHeadLookup TransactionHead,
    int BranchId,
    string BranchName,
    int? CustomerId,
    string? CustomerName,
    int? EmployeeId,
    string? EmployeeName,
    decimal NetAmount,
    string PaymentMethod,
    string Description,
    string? VendorName,
    decimal? RelatedLabourCharge = null
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
