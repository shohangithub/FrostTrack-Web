import { IProductResponse } from '../../administration/models/product.interface';
import { ICustomerResponse } from '../../common/models/customer.interface';
import { IUnitConversionResponse } from '../../common/models/unit-conversion.interface';
import { IBookingResponse } from '../../booking/models/booking.interface';

export interface IDeliveryRequest {
  id: string;
  deliveryNumber: string;
  deliveryDate: Date;
  bookingId: string;
  branchId: number;
  notes?: string;
  chargeAmount: number;
  adjustmentValue: number;
  discountAmount: number;
  paidAmount: number;
  deliveryDetails: IDeliveryDetailRequest[];
}

export interface IDeliveryDetailRequest {
  id: string;
  deliveryId: string;
  bookingDetailId: string;
  deliveryUnitId: number;
  deliveryQuantity: number;
  baseQuantity: number;
  chargeAmount: number;
  adjustmentValue: number;
}

export interface IDeliveryResponse {
  id: string;
  deliveryNumber: string;
  deliveryDate: Date;
  bookingId: string;
  booking: IBookingResponse;
  branchId: number;
  notes?: string;
  chargeAmount: number;
  adjustmentValue: number;
  discountAmount: number;
  paidAmount: number;
  deliveryDetails: IDeliveryDetailResponse[];
}

export interface IDeliveryDetailResponse {
  id: string;
  bookingDetailId: string;
  productId: number;
  productName: string;
  deliveryUnitId: number;
  deliveryUnitName: string;
  deliveryQuantity: number;
  baseQuantity: number;
  chargeAmount: number;
  adjustmentValue: number;
}

export interface IDeliveryListResponse {
  id: string;
  deliveryNumber: string;
  deliveryDate: Date;
  bookingId: string;
  bookingNumber: string;
  customerId: number;
  customerName: string;
  chargeAmount: number;
  discountAmount: number;
  paidAmount: number;
  deliveryDetails: IDeliveryDetailResponse[];
}

export interface ICustomerStockResponse {
  customerId: number;
  bookingDetailId: string;
  productId: number;
  productName: string;
  unitId: number;
  unitName: string;
  availableStock: number;
  bookingRate: number;
}
