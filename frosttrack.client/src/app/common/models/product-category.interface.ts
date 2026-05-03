import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IProductCategoryListResponse {
  id: number;
  categoryName: string;
  description?: string;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IProductCategoryPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IProductCategoryResponse {
  id: number;
  categoryName: string;
  description?: string;
  isActive: boolean;
  status: string;
}

export interface IProductCategoryRequest {
  categoryName: string;
  description?: string;
  isActive: boolean;
}
