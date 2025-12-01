export interface IBalanceSheetItem {
  accountName: string;
  accountCategory: string;
  amount: number;
  transactionCount: number;
}

export interface IBalanceSheetSummary {
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  netWorth: number;
  asOfDate: string;
  totalTransactions: number;
  assets: IBalanceSheetItem[];
  liabilities: IBalanceSheetItem[];
  equity: IBalanceSheetItem[];
}
