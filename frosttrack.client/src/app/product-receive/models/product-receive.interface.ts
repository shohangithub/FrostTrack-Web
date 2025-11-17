import { IProductResponse } from '../../administration/models/product.interface';
import { ICustomerResponse } from '../../common/models/customer.interface';
import { IUnitConversionResponse } from '../../common/models/unit-conversion.interface';

export interface IProductReceiveRequest {
  id: number;
  receiveNumber: string;
  receiveDate: Date;
  customerId: number;
  subtotal: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  totalAmount: number;
  paidAmount: number;
  branchId: number;
  notes?: string;
  productReceiveDetails: IProductReceiveDetailRequest[];
}

export interface IProductReceiveDetailRequest {
  id: number;
  productReceiveId: number;
  productId: number;
  receiveUnitId: number;
  receiveQuantity: number;
  bookingRate: number;
  receiveAmount: number;
}

export interface IProductReceiveResponse {
  id: number;
  receiveNumber: string;
  receiveDate: Date;
  customerId: number;
  customer: ICustomerResponse;
  subtotal: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  totalAmount: number;
  paidAmount: number;
  branchId: number;
  notes?: string;
  productReceiveDetails: IProductReceiveDetailResponse[];
}

export interface IProductReceiveDetailResponse {
  id: number;
  productReceiveId: number;
  productId: number;
  product: IProductResponse;
  receiveUnitId: number;
  receiveUnit: IUnitConversionResponse;
  bookingRate: number;
  receiveQuantity: number;
  receiveAmount: number;
}

export interface IProductReceiveListResponse {
  id: number;
  receiveNumber: string;
  receiveDate: Date;
  customerId: number;
  customer: ICustomerResponse;
  subtotal: number;
  vatAmount: number;
  discountPercent: number;
  discountAmount: number;
  otherCost: number;
  totalAmount: number;
  paidAmount: number;
  branchId: number;
  notes?: string;
  branch: any;
}
