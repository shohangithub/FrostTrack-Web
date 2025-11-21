export interface IDeliveryRequest {
  deliveryNumber: string;
  deliveryDate: Date;
  bookingId: string;
  notes?: string;
  chargeAmount: number;
  adjustmentValue: number;
  totalPreviousPayments?: number;
  remainingBalance?: number;
  deliveryDetails: IDeliveryDetailRequest[];
  createTransaction?: boolean;
  transactionAmount?: number;
  paymentMethod?: string;
  transactionNotes?: string;
}

export interface IDeliveryDetailRequest {
  bookingDetailId: string;
  deliveryUnitId: number;
  deliveryQuantity: number;
  baseQuantity: number;
  chargeAmount: number;
  adjustmentValue: number;
}

export interface IBookingForDeliveryResponse {
  id: string;
  bookingNumber: string;
  bookingDate: Date;
  customerId: number;
  customerName?: string;
  branchId: number;
  branchName?: string;
  notes?: string;
  lastDeliveryDate: Date;
  bookingDetails: IBookingDetailForDeliveryResponse[];
}

export interface IBookingDetailForDeliveryResponse {
  id: string;
  productId: number;
  productName?: string;
  bookingUnitId: number;
  bookingUnitName?: string;
  bookingQuantity: number;
  billType: string;
  bookingRate: number;
  baseQuantity: number;
  baseRate: number;
  totalCharge: number; // Charge per delivery unit calculated from BillType
  totalDeliveredQuantity: number;
  remainingQuantity: number;
  lastDeliveryDate: Date;
  availableUnits: IUnitConversionResponse[];
}

export interface IUnitConversionResponse {
  id: number;
  unitId: number;
  unitName?: string;
  conversionRate: number;
  isBaseUnit: boolean;
}

export interface IDeliveryResponse {
  id: string;
  deliveryNumber: string;
  deliveryDate: Date;
  bookingId: string;
  bookingNumber?: string;
  customerId: number;
  customerName?: string;
  branchId: number;
  branchName?: string;
  notes?: string;
  chargeAmount: number;
  adjustmentValue: number;
  customer?: ICustomerBasicInfo;
  booking?: IBookingBasicInfo;
  deliveryDetails: IDeliveryDetailResponse[];
}

export interface ICustomerBasicInfo {
  customerId: number;
  customerName: string;
  customerMobile?: string;
  address?: string;
}

export interface IBookingBasicInfo {
  bookingId: string;
  bookingNumber: string;
  bookingDate: Date;
  lastDeliveryDate: Date;
}

export interface IDeliveryDetailResponse {
  id: string;
  deliveryId: string;
  bookingDetailId: string;
  productId: number;
  productName?: string;
  deliveryUnitId: number;
  deliveryUnitName?: string;
  deliveryQuantity: number;
  baseQuantity: number;
  chargeAmount: number;
  adjustmentValue: number;
  bookingQuantity: number;
  totalDeliveredQuantity: number;
  remainingQuantity: number;
}

export interface IDeliveryInvoiceResponse {
  id: string;
  deliveryNumber: string;
  deliveryDate: Date;
  bookingId: string;
  bookingNumber: string;
  branchId: number;
  branchName?: string;
  notes?: string;
  chargeAmount: number;
  adjustmentValue: number;
  customer?: ICustomerBasicInfo;
  booking?: IBookingInvoiceInfo;
  deliveryDetails: IDeliveryInvoiceDetailResponse[];
  totalBookingAmount: number;
  totalPaidAmount: number;
  extraCharge: number;
  dueAmount: number;
}

export interface IBookingInvoiceInfo {
  bookingId: string;
  bookingNumber: string;
  bookingDate: Date;
  lastDeliveryDate: Date;
  totalBookingAmount: number;
}

export interface IDeliveryInvoiceDetailResponse {
  id: string;
  productId: number;
  productName: string;
  deliveryUnitId: number;
  deliveryUnitName: string;
  deliveryQuantity: number;
  baseQuantity: number;
  chargeAmount: number;
  bookingRate: number;
}
