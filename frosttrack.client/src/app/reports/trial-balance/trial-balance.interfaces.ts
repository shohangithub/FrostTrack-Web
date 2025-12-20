export interface ITrialBalanceItem {
  accountName: string;
  accountType: string;
  debitAmount: number;
  creditAmount: number;
  balance: number;
  transactionCount: number;
}

export interface ITrialBalanceSummary {
  reportDate: Date;
  openingBalance: number;
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
  totalTransactions: number;
  items: ITrialBalanceItem[];
}
