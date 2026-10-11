import {
  IBookingAdjustmentLine,
  IBookingPaymentItemLine,
  IBookingPaymentSummary,
} from 'app/bill-collection/models/booking-payment.interface';

/** Everything that happened on one booking (Booking History report). */
export interface IBookingHistory {
  bookingId: string;
  bookingNumber: string;
  referenceNumber?: string | null;
  bookingDate: string;
  isContracted: boolean;
  notes?: string | null;
  branchName?: string | null;
  status: 'RUNNING' | 'COMPLETED';
  customerId: number;
  customerName: string;
  customerMobile?: string | null;
  customerAddress?: string | null;
  items: IBookingPaymentItemLine[];
  deliveries: IBookingHistoryDelivery[];
  statement: IBookingStatementLine[];
  adjustments: IBookingAdjustmentLine[];
  summary: IBookingPaymentSummary;
  generatedAt: string;
}

export interface IBookingHistoryDelivery {
  deliveryId: string;
  deliveryNumber: string;
  deliveryDate: string;
  notes?: string | null;
  paymentStatus: string;
  challanNumber?: string | null;
  vehicleNumber?: string | null;
  driverName?: string | null;
  adjustmentValue: number;
  collectedAmount: number;
  lines: IBookingHistoryDeliveryLine[];
}

export interface IBookingHistoryDeliveryLine {
  productName: string;
  quantity: number;
  unitName: string;
  billingCycles: number;
  rentCharge: number;
  labourCharge: number;
}

export interface IBookingStatementLine {
  date: string;
  /** RENT, LABOUR, DELIVERY_ADJUSTMENT, ADJUSTMENT_PLUS, CORRECTION, PAYMENT, AT_DELIVERY, ADVANCE, ADJUSTMENT_MINUS */
  category: string;
  particulars: string;
  reference?: string | null;
  charge: number;
  paid: number;
  balance: number;
}
