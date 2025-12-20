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
