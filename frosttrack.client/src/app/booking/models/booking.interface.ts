import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { IBranchListResponse } from 'app/common/models/branch.interface';
import { IProductResponse } from 'app/administration/models/product.interface';
import { IUnitConversionResponse } from 'app/common/models/unit-conversion.interface';
import { PaginationQuery } from '@core/models/pagination-query';

export interface IBookingRequest {
  id: string;
  bookingNumber: string;
  referenceNumber?: string;
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
  labourCharge: number;
  lastDeliveryDate?: string;
}

export interface IBookingResponse {
  id: string;
  bookingNumber: string;
  referenceNumber?: string;
  bookingDate: string;
  customerId: number;
  customer: ICustomerListResponse;
  branchId: number;
  branch: IBranchListResponse;
  notes?: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
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
  labourCharge: number;
  lastDeliveryDate?: string;
}

export interface IBookingListResponse {
  id: string;
  bookingNumber: string;
  referenceNumber?: string;
  bookingDate: string;
  customerId: number;
  customer: ICustomerListResponse;
  branchId: number;
  branch: IBranchListResponse;
  notes?: string;
  isDeleted: boolean;
  isArchived: boolean;
  deletedAt?: string;
  archivedAt?: string;
  bookingDetails: IBookingDetailListResponse[];
}

export interface IBookingPaginationQuery extends PaginationQuery {
  status: 'active' | 'archived' | 'deleted';
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
  labourCharge: number;
  lastDeliveryDate?: string;
}

export interface IBookingInvoiceWithDeliveryResponse {
  id: string;
  bookingNumber: string;
  referenceNumber?: string;
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
  labourCharge: number;
}

// Customer Due Interfaces
export interface ICustomerDueSummaryResponse {
  customerId: number;
  customerName: string;
  customerMobile: string;
  customerAddress: string;
  totalBookings: number;
  totalAmount: number;
  totalPaid: number;
  totalDue: number;
  oldestBookingDate: string;
  daysSinceOldestBooking: number;
  status: 'normal' | 'warning' | 'danger';
}

export interface ICustomerDueDetailResponse {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  referenceNumber?: string;
  totalAmount: number;
  totalPaid: number;
  totalDue: number;
  daysSinceBooking: number;
  status: 'normal' | 'warning' | 'danger';
  deliveries: ICustomerDueDeliveryResponse[];
}

export interface ICustomerDueDeliveryResponse {
  deliveryId: string;
  deliveryNumber: string;
  deliveryDate: string;
  chargeAmount: number;
  labourCharge: number;
  adjustmentValue: number;
  discountAmount: number;
  paidAmount: number;
  dueAmount: number;
  deliveryDetails: IDeliveryDetailInfoResponse[];
}
