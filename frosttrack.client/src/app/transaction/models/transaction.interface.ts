import { PaginationQuery } from '@core/models/pagination-query';
import { ITransactionHeadLookup } from 'app/common/models/transaction-head.interface';

export enum PaymentMethod {
  CASH = 'CASH',
  BANK_TRANSFER = 'BANK_TRANSFER',
  CHEQUE = 'CHEQUE',
  MOBILE_BANKING = 'MOBILE_BANKING',
  CARD = 'CARD',
  OTHER = 'OTHER',
}

export interface ITransactionListResponse {
  id: string;
  transactionCode: string;
  transactionDate: Date | string;
  transactionHeadId: string;
  transactionHead: ITransactionHeadLookup | null;
  branchId: number;
  branchName: string;
  customerId?: number | null;
  customerName?: string | null;
  netAmount: number;
  paymentMethod: string;
  category?: string | null;
  description: string;
  vendorName?: string | null;
}

export interface ITransactionRequest {
  id?: string;
  transactionCode: string;
  transactionHeadId: string;
  transactionDate: Date | string;
  branchId: number;
  amount: number;
  note?: string;
  // Optional fields with defaults
  entityName?: string;
  entityId?: string;
  customerId?: number | null;
  bookingId?: string | null;
  discountAmount?: number;
  adjustmentValue?: number;
  paymentMethod?: PaymentMethod | string;
  paymentReference?: string;
  description?: string;
  vendorName?: string;
  vendorContact?: string;
  billingPeriodStart?: Date | null;
  billingPeriodEnd?: Date | null;
  attachmentPath?: string;
}

export interface IBillCollectionRequest {
  id?: string;
  transactionCode: string;
  transactionDate: Date | string;
  branchId: number;
  amount: number;
  note?: string;
  // Optional fields with defaults
  bookingId?: string | null;
  discountAmount?: number;
  adjustmentValue?: number;
  paymentMethod?: PaymentMethod | string;
  paymentReference?: string;
  description?: string;
}

export interface ITransactionDetailResponse extends ITransactionListResponse {
  amount: number;
  discountAmount: number;
  adjustmentValue: number;
  note?: string | null;
  entityName?: string;
  entityId?: string;
  bookingId?: string | null;
  paymentReference?: string | null;
  subCategory?: string | null;
  vendorContact?: string | null;
  billingPeriodStart?: Date | null;
  billingPeriodEnd?: Date | null;
  attachmentPath?: string | null;
  updatedAt?: Date;
  updatedBy?: string;
  deletedBy?: string;
  archivedBy?: string;
}

export interface ITransactionPaginationQuery extends PaginationQuery {
  usageFor: string;
}
