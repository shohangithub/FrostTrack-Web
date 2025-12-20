namespace Application.ReponseDTO;

public record BalanceSheetItemResponse
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountCategory { get; set; } = string.Empty; // Asset, Liability, Equity
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

public record BalanceSheetSummaryResponse
{
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal NetWorth { get; set; } // Assets - Liabilities
    public DateTime ReportDate { get; set; }
    public int TotalTransactions { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public List<BalanceSheetItemResponse> Assets { get; set; } = new();
    public List<BalanceSheetItemResponse> Liabilities { get; set; } = new();
    public List<BalanceSheetItemResponse> Equity { get; set; } = new();
}
