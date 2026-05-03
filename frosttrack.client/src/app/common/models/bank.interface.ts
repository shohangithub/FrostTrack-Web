import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IBankListResponse {
  id: number;
  bankName: string;
  bankCode: string;
  bankBranch?: string;
  accountNumber?: string;
  accountTitle?: string;
  swiftCode?: string;
  routingNumber?: string;
  ibanNumber?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  openingBalance: number;
  currentBalance: number;
  description?: string;
  isMainAccount: boolean;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IBankPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IBankResponse {
  id: number;
  bankName: string;
  bankCode: string;
  bankBranch?: string;
  accountNumber?: string;
  accountTitle?: string;
  swiftCode?: string;
  routingNumber?: string;
  ibanNumber?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  openingBalance: number;
  currentBalance: number;
  description?: string;
  isMainAccount: boolean;
  isActive: boolean;
  status: string;
}

export interface IBankRequest {
  id: number;
  bankName: string;
  bankCode: string;
  bankBranch?: string;
  accountNumber?: string;
  accountTitle?: string;
  swiftCode?: string;
  routingNumber?: string;
  ibanNumber?: string;
  contactPerson?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: string;
  openingBalance?: number;
  currentBalance?: number;
  description?: string;
  isMainAccount: boolean;
  isActive: boolean;
}
