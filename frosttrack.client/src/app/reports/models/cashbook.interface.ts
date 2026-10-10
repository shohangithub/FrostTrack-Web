export interface ICashBookItem {
  transactionHeadId: string;
  transactionHeadName: string;
  transactionType: string;
  transactionCount: number;
  debitAmount: number;
  creditAmount: number;
  balance: number;
}

export interface ICashBookReport {
  openingBalance: number;
  items: ICashBookItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface ICashBookDayReport {
  date: string | Date;
  openingBalance: number;
  isOpeningBalanceReset: boolean;
  openingBalanceAdjustment: number;
  items: ICashBookItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface ICashBookRangeReport {
  fromDate: string | Date;
  toDate: string | Date;
  openingBalance: number;
  days: ICashBookDayReport[];
  headSummary: ICashBookItem[];
  totalDebit: number;
  totalCredit: number;
  openingBalanceAdjustment: number;
  closingBalance: number;
}
