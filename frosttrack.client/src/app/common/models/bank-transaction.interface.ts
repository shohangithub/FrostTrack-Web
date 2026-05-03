import { PaginationQuery } from '@core/models/pagination-query';

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
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: Date;
  archivedAt?: Date;
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
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: Date;
  archivedAt?: Date;
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

export interface IBankTransactionPaginationQuery extends PaginationQuery {
  transactionType?: string;
  status?: string;
  archiveStatus?: 'active' | 'archived' | 'deleted';
}
