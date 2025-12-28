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
