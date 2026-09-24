export interface BankBalanceSummaryItem {
  bankId: number;
  bankName: string;
  accountNumber?: string;
  closingBalance: number;
}

export interface SeasonArchivePreviewResponse {
  proposedCutoffDate: string;
  completedBookingsCount: number;
  openBookingsToCarryForwardCount: number;
  totalRemainingStockBags: number;
  totalCustomersCount: number;
  totalCustomerNetDue: number;
  currentCashInHand: number;
  bankBalances: BankBalanceSummaryItem[];
  deliveriesToArchiveCount: number;
  challansToArchiveCount: number;
  transactionsToArchiveCount: number;
}

export interface ExecuteSeasonArchiveRequest {
  cutoffDate: string;
  seasonTitle: string;
  confirmationCode: string;
  notes?: string;
}

export interface SeasonArchiveResultResponse {
  archiveBatchId: string;
  batchCode: string;
  seasonTitle: string;
  cutoffDate: string;
  archivedBookingsCount: number;
  carriedForwardBookingsCount: number;
  carriedForwardStockBags: number;
  carriedForwardCustomerDue: number;
  carriedForwardCashBalance: number;
  carriedForwardBankBalance: number;
  archivedDeliveriesCount: number;
  archivedTransactionsCount: number;
  executedAt: string;
  message: string;
}

export interface SeasonArchiveHistoryResponse {
  id: string;
  batchCode: string;
  seasonTitle: string;
  cutoffDate: string;
  totalArchivedBookings: number;
  totalCarriedForwardBookings: number;
  totalCarriedForwardStock: number;
  totalCarriedForwardCustomerDue: number;
  totalCarriedForwardCashBalance: number;
  totalCarriedForwardBankBalance: number;
  totalArchivedDeliveries: number;
  totalArchivedTransactions: number;
  executedByName: string;
  executedAt: string;
  notes?: string;
}
