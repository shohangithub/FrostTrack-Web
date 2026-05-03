import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IDamageListResponse {
  id: number;
  damageNumber: string;
  damageDate: string;
  productName?: string;
  unitName?: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  reason?: string;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IDamagePaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IDamageResponse {
  id: number;
  damageNumber: string;
  damageDate: string;
  productId: number;
  productName?: string;
  unitId: number;
  unitName?: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  reason?: string;
  description?: string;
  isActive: boolean;
  status: string;
}

export interface IDamageRequest {
  damageNumber: string;
  damageDate: string;
  productId: number;
  unitId: number;
  quantity: number;
  unitCost: number;
  totalCost: number;
  reason?: string;
  description?: string;
  isActive: boolean;
}
