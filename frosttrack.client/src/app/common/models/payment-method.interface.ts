import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IPaymentMethodListResponse {
  id: number;
  methodName: string;
  code: string;
  description?: string;
  category: string;
  requiresBankAccount: boolean;
  requiresCheckDetails: boolean;
  requiresOnlineDetails: boolean;
  requiresMobileWalletDetails: boolean;
  requiresCardDetails: boolean;
  isActive: boolean;
  sortOrder: number;
  iconClass?: string;
  branchId?: number;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IPaymentMethodPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IPaymentMethodResponse {
  id: number;
  methodName: string;
  code: string;
  description?: string;
  category: string;
  requiresBankAccount: boolean;
  requiresCheckDetails: boolean;
  requiresOnlineDetails: boolean;
  requiresMobileWalletDetails: boolean;
  requiresCardDetails: boolean;
  isActive: boolean;
  sortOrder: number;
  iconClass?: string;
  branchId?: number;
  status: string;
}

export interface IPaymentMethodRequest {
  methodName: string;
  code: string;
  description?: string;
  category: string;
  requiresBankAccount: boolean;
  requiresCheckDetails: boolean;
  requiresOnlineDetails: boolean;
  requiresMobileWalletDetails: boolean;
  requiresCardDetails: boolean;
  isActive: boolean;
  sortOrder: number;
  iconClass?: string;
  branchId?: number;
}
