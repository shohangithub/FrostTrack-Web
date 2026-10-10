export interface ITrialBalanceItem {
  accountName: string;
  accountType: string;
  debitAmount: number;
  creditAmount: number;
  balance: number;
  transactionCount: number;
  sortOrder?: number;
}

export interface ITrialBalanceSummary {
  reportDate: Date | string;
  fromDate?: Date | string;
  toDate?: Date | string;
  openingBalance: number;
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
  totalTransactions: number;
  items: ITrialBalanceItem[];
}
