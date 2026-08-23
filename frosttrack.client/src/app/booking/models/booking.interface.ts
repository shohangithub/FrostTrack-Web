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
  openingBalance: number;
  totalAmount: number;
  pendingRecurringChargeAmount: number;
  totalPaid: number;
  totalDue: number;
  oldestBookingDate: string;
  daysSinceOldestBooking: number;
  lastPaymentDate?: string;
  daysSinceLastPayment: number;
  status: 'normal' | 'warning' | 'danger';
}

export interface ICustomerDueDetailResponse {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  referenceNumber?: string;
  bookingLabourCharge: number;
  openingBalance: number;
  totalAccruedAmount: number;
  pendingRecurringChargeAmount: number;
  lastDeliveryDate?: string;
  totalAmount: number; // alias for totalAccruedAmount (backward-compat)
  totalPaid: number;
  totalDue: number;
  daysSinceBooking: number;
  status: 'normal' | 'warning' | 'danger';
  deliveries: ICustomerDueDeliveryResponse[];
  recurringChargeEntries: IRecurringChargeEntryResponse[];
}

export interface IRecurringChargeEntryResponse {
  id: string;
  bookingId: string;
  bookingDetailId: string;
  productName: string;
  recurringChargeRunId?: string;
  source: string; // INITIAL | RUN
  billPeriodFrom: string;
  billPeriodTo: string;
  billType: string;
  cycles: number;
  quantity: number;
  rate: number;
  amount: number;
  note?: string;
  createdAt: string;
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

export interface ICustomerOutstandingResponse {
  customerId: number;
  customerName: string;
  customerMobile: string;
  openingBalance: number;
  totalAccrued: number;
  totalPaid: number;
  totalDue: number;
  bookings: IBookingOutstandingItem[];
}

export interface IBookingOutstandingItem {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  accruedAmount: number;
  paidAmount: number;
  dueAmount: number;
}

export interface IRecurringChargePreviewBooking {
  bookingId: string;
  bookingNumber: string;
  customerName: string;
  affectedDetailLines: number;
  totalRecurringChargeAmount: number;
  oldestLastRecurringChargeDate?: string;
}

export interface IRecurringChargePreview {
  asOfDate: string;
  totalAffectedBookings: number;
  totalAffectedDetailLines: number;
  totalRecurringChargeAmount: number;
  bookings: IRecurringChargePreviewBooking[];
}

export interface IRecurringChargeRunResponse {
  id: string;
  triggeredBy: string;
  asOfDate: string;
  status: string;
  affectedCount: number;
  totalRecurringChargeAmount: number;
  notes?: string;
  runByUserName: string;
  startedAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface IRecurringChargeRunRequest {
  asOfDate?: string;
  notes?: string;
}
