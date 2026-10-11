// Ledger Book per Transaction Group (Trial Balance sides):
// money received from the group -> Credit, money paid to the group -> Debit.
// Balances are Credit - Debit (positive = Cr, negative = Dr).

export interface ILedgerBookSummary {
  fromDate: string | Date;
  toDate: string | Date;
  groups: ILedgerBookGroupSummaryItem[];
  totalOpeningBalance: number;
  totalDebit: number;
  totalCredit: number;
  totalClosingBalance: number;
}

export interface ILedgerBookGroupSummaryItem {
  groupId: string;
  groupCode: string;
  groupName: string;
  openingBalance: number;
  debitAmount: number;
  creditAmount: number;
  closingBalance: number;
  transactionCount: number;
}

export interface ILedgerBookGroup {
  fromDate: string | Date;
  toDate: string | Date;
  groupId: string;
  groupCode: string;
  groupName: string;
  openingBalance: number;
  items: ILedgerBookItem[];
  totalDebit: number;
  totalCredit: number;
  closingBalance: number;
}

export interface ILedgerBookItem {
  id: string;
  date: string | Date;
  transactionCode: string;
  description: string;
  transactionHeadName: string;
  paymentMethod: string;
  bankName?: string | null;
  referenceNo?: string | null;
  debitAmount: number;
  creditAmount: number;
  balance: number;
}
