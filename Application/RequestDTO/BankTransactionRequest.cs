namespace Application.RequestDTO;

public record BankTransactionRequest
{
    public long Id { get; set; }
    public required string TransactionNumber { get; set; }
    public required DateTime TransactionDate { get; set; }
    public required int BankId { get; set; }
    public required string TransactionType { get; set; } // "Deposit" or "Withdraw"
    public required decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public string? ReceiptNumber { get; set; }
    public required bool IsActive { get; set; } = true;
}