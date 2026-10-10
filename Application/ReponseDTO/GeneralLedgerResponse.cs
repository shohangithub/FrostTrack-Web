namespace Application.ReponseDTO;

public class GeneralLedgerResponse
{
    public DateTime ReportDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerItemResponse> Items { get; set; } = new();
    /// <summary>Period totals per account (Transaction Group, else head / bank account): Debit and Credit side by side.</summary>
    public List<GeneralLedgerAccountSummaryResponse> AccountSummary { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class GeneralLedgerDayResponse
{
    public DateTime Date { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsOpeningBalanceReset { get; set; }
    public decimal OpeningBalanceAdjustment { get; set; }
    public List<GeneralLedgerItemResponse> Items { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class GeneralLedgerRangeResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerDayResponse> Days { get; set; } = new();
    /// <summary>Period totals per account (Transaction Group, else head / bank account): Debit and Credit side by side.</summary>
    public List<GeneralLedgerAccountSummaryResponse> AccountSummary { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal OpeningBalanceAdjustment { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class GeneralLedgerItemResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    /// <summary>Transaction head name (null for bank / contra lines). Shown under AccountName when the head is grouped.</summary>
    public string? HeadName { get; set; }
    public string? GroupName { get; set; }
    public string AccountType { get; set; } = string.Empty; // "Cash" or "Bank"
    public string TransactionType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}

public class GeneralLedgerAccountSummaryResponse
{
    public string AccountName { get; set; } = string.Empty;
    public bool IsGroup { get; set; }
    public int TransactionCount { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}
