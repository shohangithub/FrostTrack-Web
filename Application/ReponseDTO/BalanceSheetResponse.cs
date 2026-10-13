namespace Application.ReponseDTO;

public static class BalanceSheetSections
{
    public const string NonCurrentAssets = "Non-current assets";
    public const string CurrentAssets = "Current assets";
    public const string CurrentLiabilities = "Current liabilities";
    public const string Equity = "Equity";
    public const string Profit = "Profit";
}

public record BalanceSheetItemResponse
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountCategory { get; set; } = string.Empty; // Asset, Liability, Equity, Profit
    public string Section { get; set; } = string.Empty;         // see BalanceSheetSections
    public decimal Amount { get; set; }
    public int TransactionCount { get; set; }
}

public record BalanceSheetSummaryResponse
{
    /// <summary>False until the Balance Sheet start date is set (Opening Balances page).</summary>
    public bool IsConfigured { get; set; }
    /// <summary>Why the report is empty (not configured / date before the start date).</summary>
    public string? Message { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? FiscalYearStart { get; set; }

    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal NetWorth { get; set; } // Assets - Liabilities
    /// <summary>Assets − (Liabilities + Equity). Non-zero means figures are missing or wrong.</summary>
    public decimal Difference { get; set; }
    /// <summary>Net profit / (loss) from the fiscal year start (or the start date) to the report date.</summary>
    public decimal CurrentYearProfit { get; set; }
    public DateTime ReportDate { get; set; }
    public int TotalTransactions { get; set; }
    public List<BalanceSheetItemResponse> Assets { get; set; } = new();
    public List<BalanceSheetItemResponse> Liabilities { get; set; } = new();
    public List<BalanceSheetItemResponse> Equity { get; set; } = new();
    /// <summary>How the current year's net profit is made up.</summary>
    public List<BalanceSheetItemResponse> ProfitItems { get; set; } = new();
}

public record ComparativeBalanceSheetItemResponse
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountCategory { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
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
