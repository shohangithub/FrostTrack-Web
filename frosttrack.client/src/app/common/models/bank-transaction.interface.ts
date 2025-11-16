export interface IBankTransactionListResponse {
  id: number;
  transactionNumber: string;
  transactionDate: Date;
  bankId: number;
  bankName: string;
  transactionType: string;
  amount: number;
  reference?: string;
  description?: string;
  balanceAfter: number;
  receiptNumber?: string;
  status: string;
}

export interface IBankTransactionResponse {
  id: number;
  transactionNumber: string;
  transactionDate: Date;
  bankId: number;
  bankName: string;
  transactionType: string;
  amount: number;
  reference?: string;
  description?: string;
  balanceAfter: number;
  receiptNumber?: string;
  isActive: boolean;
  status: string;
}

export interface IBankTransactionRequest {
  id: number;
  transactionNumber: string;
  transactionDate: Date;
  bankId: number;
  transactionType: string;
  amount: number;
  reference?: string;
  description?: string;
  receiptNumber?: string;
  isActive: boolean;
}
