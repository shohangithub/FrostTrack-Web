export interface IBalanceSheetItem {
  accountName: string;
  accountCategory: string;
  section: string;
  amount: number;
  transactionCount: number;
}

export interface IBalanceSheetSummary {
  /** False until the balance sheet start date is set on the setup page. */
  isConfigured: boolean;
  /** Why the report is empty (not configured / date before the start date). */
  message?: string | null;
  startDate?: Date | string | null;
  fiscalYearStart?: Date | string | null;
  reportDate: Date | string;
  totalAssets: number;
  totalLiabilities: number;
  totalEquity: number;
  netWorth: number;
  /** Assets − (Liabilities + Equity); non-zero means figures are missing or wrong. */
  difference: number;
  currentYearProfit: number;
  totalTransactions: number;
  assets: IBalanceSheetItem[];
  liabilities: IBalanceSheetItem[];
  equity: IBalanceSheetItem[];
  profitItems: IBalanceSheetItem[];
}

export interface IComparativeBalanceSheetItem {
  accountName: string;
  accountCategory: string;
  section: string;
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

export interface IBalanceSheetSectionGroup<T> {
  section: string;
  items: T[];
}
