namespace Application.ReponseDTO;

public class BankBookResponse
{
    public DateTime ReportDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<BankBookItemResponse> Items { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class BankBookItemResponse
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal Balance { get; set; }
}
