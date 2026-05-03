using Domain.Entitites;

namespace Application.ReponseDTO;

public record TransactionResponse(
    Guid Id,
    string TransactionCode,
    DateTime TransactionDate,
    Guid TransactionHeadId,
    TransactionHeadLookup TransactionHead,
    int BranchId,
    Branch? Branch,
    int? CustomerId,
    Customer? Customer,
    Guid? BookingId,
    Booking? Booking,
        Guid? DeliveryId,
        int? SupplierId,
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
    decimal? RelatedLabourCharge = null,
    decimal? Bonus = null,
    decimal? Deduction = null
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
    bool IsDeleted = false,
    bool IsArchived = false,
    DateTime? DeletedAt = null,
    DateTime? ArchivedAt = null,
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
