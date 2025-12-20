export interface ILedgerBookResponse {
  reportDate: Date;
  openingBalance: number;
  items: ILedgerBookItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface ILedgerBookItem {
  id: string;
  date: Date;
  transactionCode: string;
  description: string;
  transactionHeadName: string;
  transactionType: string;
  paymentMethod: string;
  referenceNo?: string;
  debitAmount: number;
  creditAmount: number;
  balance: number;
}

export interface IEntityOption {
  id: number;
  name: string;
  code: string;
}
