import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IBaseUnitListResponse {
  id: number;
  unitName: string;
  description?: string;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IBaseUnitPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IBaseUnitResponse {
  id: number;
  unitName: string;
  description?: string;
  isActive: boolean;
  status: string;
}

export interface IBaseUnitRequest {
  unitName: string;
  description?: string;
  isActive: boolean;
}
