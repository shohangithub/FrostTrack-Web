namespace Application.ReponseDTO
{
    // Ledger Book is an account ledger per Transaction Group, using Trial Balance sides:
    //   money received from the group (head Type DEBIT / IN)  -> Credit
    //   money paid to the group       (head Type CREDIT / OUT) -> Debit
    // Balance is signed as Credit - Debit (positive = Cr, negative = Dr).

    public class LedgerBookSummaryResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<LedgerBookGroupSummaryItem> Groups { get; set; } = new();
        public decimal TotalOpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalClosingBalance { get; set; }
    }

    public class LedgerBookGroupSummaryItem
    {
        public Guid GroupId { get; set; }
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public int TransactionCount { get; set; }
    }

    public class LedgerBookGroupResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Guid GroupId { get; set; }
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public List<LedgerBookItemResponse> Items { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class LedgerBookItemResponse
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TransactionHeadName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? ReferenceNo { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
    }
}
