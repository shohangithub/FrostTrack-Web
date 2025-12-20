export interface IBalanceSheetItem {
  accountName: string;
  accountCategory: string;
  amount: number;
  transactionCount: number;
}

export interface IBalanceSheetSummary {
  reportDate: Date;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  netWorth: number;
  totalTransactions: number;
  assets: IBalanceSheetItem[];
  liabilities: IBalanceSheetItem[];
  equity: IBalanceSheetItem[];
}
