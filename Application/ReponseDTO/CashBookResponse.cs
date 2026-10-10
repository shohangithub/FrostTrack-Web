namespace Application.ReponseDTO
{
    public class CashBookItemResponse
    {
        public Guid TransactionHeadId { get; set; }
        public string TransactionHeadName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        /// <summary>True when this row merges heads of one Transaction Group (TransactionHeadId then holds the group id).</summary>
        public bool IsGroup { get; set; }
        public int TransactionCount { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
    }

    public class CashBookResponse
    {
        public decimal OpeningBalance { get; set; }
        public List<CashBookItemResponse> Items { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    /// <summary>One calendar day inside a date-range Cash Book.</summary>
    public class CashBookDayResponse
    {
        public DateTime Date { get; set; }
        public decimal OpeningBalance { get; set; }
        /// <summary>True when an explicit opening balance (e.g. season carry-forward) re-anchored cash on this day.</summary>
        public bool IsOpeningBalanceReset { get; set; }
        /// <summary>Difference between the re-anchored opening and the carried running balance (0 when no reset).</summary>
        public decimal OpeningBalanceAdjustment { get; set; }
        public List<CashBookItemResponse> Items { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class CashBookRangeResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<CashBookDayResponse> Days { get; set; } = new();
        /// <summary>Head-wise totals for the whole period (Balance is not applicable and is 0).</summary>
        public List<CashBookItemResponse> HeadSummary { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        /// <summary>Sum of opening-balance re-anchors inside the period (0 in normal operation).</summary>
        public decimal OpeningBalanceAdjustment { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
