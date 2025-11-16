import { IProductResponse } from "../../administration/models/product.interface";
import { ICustomerResponse } from "../../common/models/customer.interface";
import { IUnitConversionResponse } from "../../common/models/unit-conversion.interface";

export interface ISalesRequest {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  salesType: string;
  customerId: number;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  invoiceAmount: number;
  paidAmount: number;
  branchId: number;
  salesDetails: ISalesDetailRequest[];
}

export interface ISalesDetailRequest {
  id: number;
  salesId: number;
  productId: number;
  salesUnitId: number;
  salesQuantity: number;
  salesAmount: number;
}
export interface ISalesResponse {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  salesType: string;
  customerId: number;
  customer: ICustomerResponse;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  invoiceAmount: number;
  paidAmount: number;
  branchId: number;
  salesDetails: ISalesDetailResponse[];
}

export interface ISalesDetailResponse {
  id: number;
  salesId: number;
  productId: number;
  product: IProductResponse;
  salesUnitId: number;
  salesUnit: IUnitConversionResponse;
  salesRate: number;
  salesQuantity: number;
  salesAmount: number;
}

export interface ISalesListResponse {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  salesType: string;
  customerId: number;
  customer: ICustomerResponse;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  invoiceAmount: number;
  paidAmount: number;
  branchId: number;
  branch: any;
}
