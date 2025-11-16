import { IProductResponse } from "../../administration/models/product.interface";
import { ISupplierResponse } from "../../common/models/supplier.interface";
import { IUnitConversionResponse } from "../../common/models/unit-conversion.interface";

export interface IPurchaseRequest {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  supplierId: number;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  invoiceAmount: number;
  paidAmount: number;
  branchId: number;
  purchaseDetails: IPurchaseDetailRequest[];
}

export interface IPurchaseDetailRequest {
  id: number;
  purchaseId: number;
  productId: number;
  purchaseUnitId: number;
  purchaseQuantity: number;
  purchaseAmount: number;
}
export interface IPurchaseResponse {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  supplierId: number;
  supplier: ISupplierResponse;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  invoiceAmount: number;
  paidAmount: number;
  branchId: number;
  purchaseDetails: IPurchaseDetailResponse[];
}

export interface IPurchaseDetailResponse {
  id: number;
  purchaseId: number;
  productId: number;
  product: IProductResponse;
  purchaseUnitId: number;
  purchaseUnit: IUnitConversionResponse;
  purchaseRate: number;
  purchaseQuantity: number;
  purchaseAmount: number;
}

export interface IPurchaseListResponse {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  supplierId: number;
  supplier: ISupplierResponse;
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
