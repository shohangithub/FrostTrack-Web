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
