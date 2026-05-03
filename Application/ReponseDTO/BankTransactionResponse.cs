namespace Application.ReponseDTO;

public record BankTransactionListResponse
(
    long Id,
    string TransactionNumber,
    DateTime TransactionDate,
    int BankId,
    string BankName,
    string TransactionType,
    decimal Amount,
    string? Reference,
    string? Description,
    decimal BalanceAfter,
    string? ReceiptNumber,
    bool IsDeleted,
    bool IsArchived,
    DateTime? DeletedAt,
    DateTime? ArchivedAt,
    string Status
);

public record BankTransactionResponse
(
    long Id,
    string TransactionNumber,
    DateTime TransactionDate,
    int BankId,
    string BankName,
    string TransactionType,
    decimal Amount,
    string? Reference,
    string? Description,
    decimal BalanceAfter,
    string? ReceiptNumber,
    bool IsActive,
    bool IsDeleted,
    bool IsArchived,
    DateTime? DeletedAt,
    DateTime? ArchivedAt,
    string Status
);