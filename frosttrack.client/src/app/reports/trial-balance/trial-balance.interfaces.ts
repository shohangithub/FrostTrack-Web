export interface ITrialBalanceItem {
  accountName: string;
  accountType: string;
  debitAmount: number;
  creditAmount: number;
  balance: number;
  transactionCount: number;
}

export interface ITrialBalanceSummary {
  totalDebit: number;
  totalCredit: number;
  netBalance: number;
  startDate: string;
  endDate: string;
  totalTransactions: number;
  items: ITrialBalanceItem[];
}
