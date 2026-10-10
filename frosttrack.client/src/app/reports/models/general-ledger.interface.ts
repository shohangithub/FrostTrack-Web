export interface IGeneralLedgerReport {
  reportDate: Date;
  openingBalance: number;
  items: IGeneralLedgerItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface IGeneralLedgerItem {
  id: string;
  date: Date;
  transactionCode: string;
  description: string;
  accountName: string;
  accountType: string; // "Cash" or "Bank"
  transactionType: string;
  paymentMethod: string;
  referenceNo?: string;
  debitAmount: number;
  creditAmount: number;
}

export interface IGeneralLedgerDayReport {
  date: string | Date;
  openingBalance: number;
  isOpeningBalanceReset: boolean;
  openingBalanceAdjustment: number;
  items: IGeneralLedgerItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface IGeneralLedgerRangeReport {
  fromDate: string | Date;
  toDate: string | Date;
  openingBalance: number;
  days: IGeneralLedgerDayReport[];
  totalDebit: number;
  totalCredit: number;
  openingBalanceAdjustment: number;
  closingBalance: number;
}
