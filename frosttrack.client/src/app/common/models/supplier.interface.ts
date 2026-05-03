import { PaginationQuery } from 'app/core/models/pagination-query';

export interface ISupplierListResponse {
  id: number;
  supplierName: string;
  supplierCode: string;
  supplierBarcode?: string;
  supplierMobile?: string;
  supplierEmail?: string;
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

export interface ISupplierPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface ISupplierResponse {
  id: number;
  supplierName: string;
  supplierCode: string;
  supplierBarcode?: string;
  supplierMobile?: string;
  supplierEmail?: string;
  officePhone?: string;
  address?: string;
  imageUrl?: string;
  creditLimit: number;
  openingBalance: number;
  isActive: boolean;
  status: string;
}

export interface ISupplierRequest {
  id: number;
  supplierName: string;
  supplierCode: string;
  supplierBarcode?: string;
  supplierMobile?: string;
  supplierEmail?: string;
  officePhone?: string;
  address?: string;
  imageUrl?: string;
  creditLimit?: number;
  openingBalance?: number;
  isActive: boolean;
}
