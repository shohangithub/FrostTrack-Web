namespace Application.ReponseDTO;

public record TrialBalanceItemResponse
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}

public record TrialBalanceSummaryResponse
{
    public DateTime ReportDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public int TotalTransactions { get; set; }
    public List<TrialBalanceItemResponse> Items { get; set; } = new();
}
