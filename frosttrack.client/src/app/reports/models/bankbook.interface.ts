export interface IBankBookReport {
  reportDate: Date;
  openingBalance: number;
  items: IBankBookItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface IBankBookItem {
  id: number;
  date: Date;
  transactionCode: string;
  description: string;
  bankName: string;
  accountNumber: string;
  transactionType: string;
  referenceNo?: string;
  debitAmount: number;
  creditAmount: number;
  balance: number;
}

export interface IBankBookDayReport {
  date: string | Date;
  openingBalance: number;
  items: IBankBookItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface IBankBookRangeReport {
  fromDate: string | Date;
  toDate: string | Date;
  bankId?: number | null;
  bankName?: string | null;
  accountNumber?: string | null;
  openingBalance: number;
  days: IBankBookDayReport[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}
