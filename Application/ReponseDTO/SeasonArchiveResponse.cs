namespace Application.ReponseDTO;

public class SeasonArchivePreviewResponse
{
    public DateTime ProposedCutoffDate { get; set; }
    public int CompletedBookingsCount { get; set; }
    public int OpenBookingsToCarryForwardCount { get; set; }
    public decimal TotalRemainingStockBags { get; set; }
    public int TotalCustomersCount { get; set; }
    public decimal TotalCustomerNetDue { get; set; }
    public decimal CurrentCashInHand { get; set; }
    public List<BankBalanceSummaryItem> BankBalances { get; set; } = new();
    public int DeliveriesToArchiveCount { get; set; }
    public int ChallansToArchiveCount { get; set; }
    public int TransactionsToArchiveCount { get; set; }
}

public class BankBalanceSummaryItem
{
    public int BankId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class ExecuteSeasonArchiveRequest
{
    public DateTime CutoffDate { get; set; }
    public string SeasonTitle { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class SeasonArchiveResultResponse
{
    public Guid ArchiveBatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string SeasonTitle { get; set; } = string.Empty;
    public DateTime CutoffDate { get; set; }
    public int ArchivedBookingsCount { get; set; }
    public int CarriedForwardBookingsCount { get; set; }
    public decimal CarriedForwardStockBags { get; set; }
    public decimal CarriedForwardCustomerDue { get; set; }
    public decimal CarriedForwardCashBalance { get; set; }
    public decimal CarriedForwardBankBalance { get; set; }
    public int ArchivedDeliveriesCount { get; set; }
    public int ArchivedTransactionsCount { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SeasonArchiveHistoryResponse
{
    public Guid Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string SeasonTitle { get; set; } = string.Empty;
    public DateTime CutoffDate { get; set; }
    public int TotalArchivedBookings { get; set; }
    public int TotalCarriedForwardBookings { get; set; }
    public decimal TotalCarriedForwardStock { get; set; }
    public decimal TotalCarriedForwardCustomerDue { get; set; }
    public decimal TotalCarriedForwardCashBalance { get; set; }
    public decimal TotalCarriedForwardBankBalance { get; set; }
    public int TotalArchivedDeliveries { get; set; }
    public int TotalArchivedTransactions { get; set; }
    public string ExecutedByName { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public string? Notes { get; set; }
}
