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

public record ComparativeBalanceSheetItemResponse
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountCategory { get; set; } = string.Empty;
    public decimal Date1Amount { get; set; }
    public decimal Date2Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercentage { get; set; }
}

public record BalanceSheetComparativeResponse
{
    public DateTime Date1 { get; set; }
    public DateTime Date2 { get; set; }
    public BalanceSheetSummaryResponse Sheet1 { get; set; } = null!;
    public BalanceSheetSummaryResponse Sheet2 { get; set; } = null!;
    public List<ComparativeBalanceSheetItemResponse> Assets { get; set; } = new();
    public List<ComparativeBalanceSheetItemResponse> Liabilities { get; set; } = new();
    public List<ComparativeBalanceSheetItemResponse> Equity { get; set; } = new();
}
