export interface IBalanceSheetItem {
  accountName: string;
  accountCategory: string;
  amount: number;
  transactionCount: number;
}

export interface IBalanceSheetSummary {
  reportDate: Date | string;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  netWorth: number;
  totalTransactions: number;
  assets: IBalanceSheetItem[];
  liabilities: IBalanceSheetItem[];
  equity: IBalanceSheetItem[];
}

export interface IComparativeBalanceSheetItem {
  accountName: string;
  accountCategory: string;
  date1Amount: number;
  date2Amount: number;
  changeAmount: number;
  changePercentage: number;
}

export interface IBalanceSheetComparative {
  date1: Date | string;
  date2: Date | string;
  sheet1: IBalanceSheetSummary;
  sheet2: IBalanceSheetSummary;
  assets: IComparativeBalanceSheetItem[];
  liabilities: IComparativeBalanceSheetItem[];
  equity: IComparativeBalanceSheetItem[];
}
