export interface IGeneralLedgerReport {
  reportDate: Date;
  openingBalance: number;
  items: IGeneralLedgerItem[];
  accountSummary: IGeneralLedgerAccountSummary[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface IGeneralLedgerAccountSummary {
  accountName: string;
  isGroup: boolean;
  transactionCount: number;
  debitAmount: number;
  creditAmount: number;
}

export interface IGeneralLedgerItem {
  id: string;
  date: Date;
  transactionCode: string;
  description: string;
  accountName: string;
  headName?: string | null;
  groupName?: string | null;
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
  accountSummary: IGeneralLedgerAccountSummary[];
  totalDebit: number;
  totalCredit: number;
  openingBalanceAdjustment: number;
  closingBalance: number;
}
