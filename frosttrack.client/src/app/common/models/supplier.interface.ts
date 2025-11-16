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
