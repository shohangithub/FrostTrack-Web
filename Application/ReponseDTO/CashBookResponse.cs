namespace Application.ReponseDTO
{
    public class CashBookItemResponse
    {
        public Guid TransactionHeadId { get; set; }
        public string TransactionHeadName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
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
}
