export interface IDueBookingOption {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  isContracted: boolean;
  due: number;
}

export interface ICustomerDueBookings {
  customerId: number;
  customerName: string;
  customerMobile?: string;
  advanceBalance: number;
  bookings: IDueBookingOption[];
}

export interface IBookingPaymentItemLine {
  productName: string;
  unitName: string;
  billType: string;
  rate: number;
  bookedQty: number;
  deliveredQty: number;
  stockInStore: number;
  currentMonth: number;
  stockRent: number;
  deliveredRent: number;
  bookingLabour: number;
  deliveryLabour: number;
}

export interface IBookingPaymentDeliveryLine {
  deliveryNumber: string;
  deliveryDate: string;
  productName: string;
  quantity: number;
  unitName: string;
  billingCycles: number;
  rentCharge: number;
  labourCharge: number;
  total: number;
  collectedAmount: number;
}

export type BookingPaymentKind = 'AT_DELIVERY' | 'PAYMENT' | 'ADVANCE' | 'DISCOUNT';

export interface IBookingPaymentHistoryLine {
  id: string;
  date: string;
  referenceCode: string;
  kind: BookingPaymentKind;
  paymentMethod?: string;
  description?: string;
  amount: number;
}

export interface IBookingPaymentSummary {
  rentCharge: number;
  labourCharge: number;
  adjustment: number;
  totalCharge: number;
  paidAtDelivery: number;
  otherPayments: number;
  advanceApplied: number;
  discount: number;
  due: number;
}

export interface IBookingPaymentDetail {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  isContracted: boolean;
  notes?: string;
  customerId: number;
  customerName: string;
  customerMobile?: string;
  customerAddress?: string;
  items: IBookingPaymentItemLine[];
  deliveries: IBookingPaymentDeliveryLine[];
  payments: IBookingPaymentHistoryLine[];
  advanceApplications: IBookingPaymentHistoryLine[];
  discounts: IBookingPaymentHistoryLine[];
  ledgerEntries: IChargeLedgerEntry[];
  summary: IBookingPaymentSummary;
  availableAdvance: number;
}

export interface IBookingPaymentRequest {
  transactionCode: string;
  transactionDate: string;
  branchId: number;
  customerId: number;
  bookingId: string;
  cashAmount: number;
  paymentMethod: string;
  paymentReference?: string | null;
  bankId?: number | null;
  note?: string | null;
  advanceAmount: number;
  discountAmount: number;
  discountReason?: string | null;
}

export interface IBookingPaymentResult {
  receiptCode: string;
  paymentTransactionId?: string | null;
  discountTransactionId?: string | null;
  cashAmount: number;
  advanceApplied: number;
  discount: number;
  dueAfter: number;
}

export interface ICustomerAdvanceLine {
  transactionId: string;
  transactionCode: string;
  date: string;
  paymentMethod?: string;
  amount: number;
  applied: number;
  remaining: number;
}

export interface IAdvanceAllocationLine {
  id: string;
  date: string;
  receiptCode?: string;
  advanceCode: string;
  bookingNumber: string;
  amount: number;
}

export interface ICustomerAdvances {
  customerId: number;
  customerName: string;
  totalReceived: number;
  totalApplied: number;
  advanceBalance: number;
  advances: ICustomerAdvanceLine[];
  allocations: IAdvanceAllocationLine[];
}

export interface IPaymentReceiptInfo {
  bookingNumber?: string;
  cashAmount: number;
  advanceApplied: number;
  discount: number;
  bookingDueNow: number;
}

/** Charge-ledger row (one dated charge: rent month, labour, adjustment or correction). */
export interface IChargeLedgerEntry {
  id: string;
  entryDate: string;
  chargeType: string; // RENT_CYCLE | BOOKING_LABOUR | DELIVERY_LABOUR | DELIVERY_ADJUSTMENT
  source: string; // POSTED | CORRECTION
  cycleNo?: number | null;
  bookingDetailId?: string | null;
  bookingNumber?: string | null;
  productName: string;
  deliveryNumber?: string | null;
  quantity: number;
  rate: number;
  amount: number;
  note?: string | null;
}
