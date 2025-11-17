import { IProductResponse } from '../../administration/models/product.interface';
import { ICustomerResponse } from '../../common/models/customer.interface';
import { IUnitConversionResponse } from '../../common/models/unit-conversion.interface';

export interface IProductDeliveryRequest {
  id: number;
  deliveryNumber: string;
  deliveryDate: Date;
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
  productDeliveryDetails: IProductDeliveryDetailRequest[];
}

export interface IProductDeliveryDetailRequest {
  id: number;
  productDeliveryId: number;
  productId: number;
  deliveryUnitId: number;
  deliveryQuantity: number;
  bookingRate: number;
  deliveryAmount: number;
}

export interface IProductDeliveryResponse {
  id: number;
  deliveryNumber: string;
  deliveryDate: Date;
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
  productDeliveryDetails: IProductDeliveryDetailResponse[];
}

export interface IProductDeliveryDetailResponse {
  id: number;
  productDeliveryId: number;
  productId: number;
  product: IProductResponse;
  deliveryUnitId: number;
  deliveryUnit: IUnitConversionResponse;
  bookingRate: number;
  deliveryQuantity: number;
  deliveryAmount: number;
  availableStock: number;
}

export interface IProductDeliveryListResponse {
  id: number;
  deliveryNumber: string;
  deliveryDate: Date;
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
