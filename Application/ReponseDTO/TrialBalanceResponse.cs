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
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal NetBalance { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalTransactions { get; set; }
    public List<TrialBalanceItemResponse> Items { get; set; } = new();
}
