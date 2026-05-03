import { PaginationQuery } from 'app/core/models/pagination-query';

export interface ICustomerListResponse {
  id: number;
  customerName: string;
  customerCode: string;
  customerBarcode?: string;
  customerMobile?: string;
  customerEmail?: string;
  officePhone?: string;
  address?: string;
  imageUrl?: string;
  creditLimit: number;
  openingBalance: number;
  previousDue: number;
  isSystemDefault: boolean;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface ICustomerPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface ICustomerResponse {
  id: number;
  customerName: string;
  customerCode: string;
  customerBarcode?: string;
  customerMobile?: string;
  customerEmail?: string;
  officePhone?: string;
  address?: string;
  imageUrl?: string;
  creditLimit: number;
  openingBalance: number;
  isActive: boolean;
  status: string;
}

export interface ICustomerRequest {
  id: number;
  customerName: string;
  customerCode: string;
  customerBarcode?: string;
  customerMobile?: string;
  customerEmail?: string;
  officePhone?: string;
  address?: string;
  imageUrl?: string;
  creditLimit?: number;
  openingBalance?: number;
  isActive: boolean;
}
