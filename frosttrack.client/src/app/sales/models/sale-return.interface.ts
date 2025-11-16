import { IProductResponse } from '../../administration/models/product.interface';
import { ICustomerResponse } from '../../common/models/customer.interface';
import { IUnitConversionResponse } from '../../common/models/unit-conversion.interface';

export interface ISaleReturnRequest {
  id: number;
  returnNumber: string;
  returnDate: Date;
  salesId: number;
  customerId: number;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  returnAmount: number;
  branchId: number;
  reason: string;
  saleReturnDetails: ISaleReturnDetailRequest[];
}

export interface ISaleReturnDetailRequest {
  id: number;
  saleReturnId: number;
  productId: number;
  returnUnitId: number;
  returnQuantity: number;
  returnAmount: number;
  reason: string;
}

export interface ISaleReturnResponse {
  id: number;
  returnNumber: string;
  returnDate: Date;
  salesId: number;
  customerId: number;
  customer: ICustomerResponse;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  returnAmount: number;
  branchId: number;
  reason: string;
  saleReturnDetails: ISaleReturnDetailResponse[];
}

export interface ISaleReturnDetailResponse {
  id: number;
  saleReturnId: number;
  productId: number;
  product: IProductResponse;
  returnUnitId: number;
  returnUnit: IUnitConversionResponse;
  returnRate: number;
  returnQuantity: number;
  returnAmount: number;
  reason: string;
}

export interface ISaleReturnListResponse {
  id: number;
  returnNumber: string;
  returnDate: Date;
  salesId: number;
  customerId: number;
  customer: ICustomerResponse;
  subtotal: number;
  vatPercent: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  returnAmount: number;
  branchId: number;
  branch: any;
  reason: string;
}
