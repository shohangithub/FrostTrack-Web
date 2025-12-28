namespace Application.ReponseDTO;

public class GeneralLedgerResponse
{
    public DateTime ReportDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerItemResponse> Items { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class GeneralLedgerItemResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // "Cash" or "Bank"
    public string TransactionType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}
