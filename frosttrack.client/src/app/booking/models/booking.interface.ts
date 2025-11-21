import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { IBranchListResponse } from 'app/common/models/branch.interface';
import { IProductResponse } from 'app/administration/models/product.interface';
import { IUnitConversionResponse } from 'app/common/models/unit-conversion.interface';

export interface IBookingRequest {
  id: string;
  bookingNumber: string;
  bookingDate: string;
  customerId: number;
  branchId: number;
  notes?: string;
  bookingDetails: IBookingDetailRequest[];
}

export interface IBookingDetailRequest {
  id: string;
  bookingId: string;
  productId: number;
  bookingUnitId: number;
  bookingQuantity: number;
  bookingRate: number;
  baseQuantity: number;
  baseRate: number;
  lastDeliveryDate?: string;
}

export interface IBookingResponse {
  id: string;
  bookingNumber: string;
  bookingDate: string;
  customerId: number;
  customer: ICustomerListResponse;
  branchId: number;
  branch: IBranchListResponse;
  notes?: string;
  bookingDetails: IBookingDetailResponse[];
}

export interface IBookingDetailResponse {
  id: string;
  bookingId: string;
  productId: number;
  product: IProductResponse;
  bookingUnitId: number;
  bookingUnit: IUnitConversionResponse;
  bookingQuantity: number;
  billType: string;
  bookingRate: number;
  baseQuantity: number;
  baseRate: number;
  lastDeliveryDate?: string;
}

export interface IBookingListResponse {
  id: string;
  bookingNumber: string;
  bookingDate: string;
  customerId: number;
  customer: ICustomerListResponse;
  branchId: number;
  branch: IBranchListResponse;
  notes?: string;
  bookingDetails: IBookingDetailListResponse[];
}

export interface IBookingDetailListResponse {
  id: string;
  bookingId: string;
  productId: number;
  productName: string;
  bookingUnitId: number;
  bookingUnitName: string;
  unitName: string;
  bookingQuantity: number;
  billType: string;
  bookingRate: number;
  baseQuantity: number;
  baseRate: number;
  lastDeliveryDate?: string;
}

export interface IBookingInvoiceWithDeliveryResponse {
  id: string;
  bookingNumber: string;
  bookingDate: string;
  customerId: number;
  customer: ICustomerListResponse;
  branchId: number;
  branch: IBranchListResponse;
  notes?: string;
  bookingDetails: IBookingDetailResponse[];
  deliveries: IDeliveryInfoResponse[];
}

export interface IDeliveryInfoResponse {
  id: string;
  deliveryNumber: string;
  deliveryDate: string;
  chargeAmount: number;
  adjustmentValue: number;
  deliveryDetails: IDeliveryDetailInfoResponse[];
}

export interface IDeliveryDetailInfoResponse {
  id: string;
  productId: number;
  productName: string;
  deliveryUnitId: number;
  deliveryUnitName: string;
  deliveryQuantity: number;
  baseQuantity: number;
  chargeAmount: number;
}
