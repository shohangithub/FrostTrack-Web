import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IUnitConversionListResponse {
  id: number;
  unitName: string;
  baseUnitId: number;
  conversionValue: number;
  baseUnitName: string;
  description?: string;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IUnitConversionPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IUnitConversionResponse {
  id: number;
  unitName: string;
  baseUnitId: number;
  conversionValue: number;
  description?: string;
  isActive: boolean;
  status: string;
}

export interface IUnitConversionRequest {
  unitName: string;
  conversionValue: number;
  baseUnitId: number;
  description?: string;
  isActive: boolean;
}
