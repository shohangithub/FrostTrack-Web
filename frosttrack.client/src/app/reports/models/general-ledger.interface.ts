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
