import { PaginationQuery } from 'app/core/models/pagination-query';

export interface IAssetListResponse {
  id: number;
  assetName: string;
  assetCode: string;
  assetType?: string;
  serialNumber?: string;
  model?: string;
  manufacturer?: string;
  purchaseDate?: Date;
  purchaseCost: number;
  currentValue: number;
  depreciationRate: number;
  location?: string;
  department?: string;
  assignedTo?: string;
  condition?: string;
  warrantyExpiryDate?: Date;
  maintenanceDate?: Date;
  notes?: string;
  imageUrl?: string;
  status: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
}

export interface IAssetPaginationQuery extends PaginationQuery {
  status?: 'active' | 'archived' | 'deleted';
}

export interface IAssetResponse {
  id: number;
  assetName: string;
  assetCode: string;
  assetType?: string;
  serialNumber?: string;
  model?: string;
  manufacturer?: string;
  purchaseDate?: Date;
  purchaseCost: number;
  currentValue: number;
  depreciationRate: number;
  location?: string;
  department?: string;
  assignedTo?: string;
  condition?: string;
  warrantyExpiryDate?: Date;
  maintenanceDate?: Date;
  notes?: string;
  imageUrl?: string;
  isActive: boolean;
  status: string;
}

export interface IAssetRequest {
  id: number;
  assetName: string;
  assetCode: string;
  assetType?: string;
  serialNumber?: string;
  model?: string;
  manufacturer?: string;
  purchaseDate?: Date;
  purchaseCost?: number;
  currentValue?: number;
  depreciationRate?: number;
  location?: string;
  department?: string;
  assignedTo?: string;
  condition?: string;
  warrantyExpiryDate?: Date;
  maintenanceDate?: Date;
  notes?: string;
  imageUrl?: string;
  isActive: boolean;
}
