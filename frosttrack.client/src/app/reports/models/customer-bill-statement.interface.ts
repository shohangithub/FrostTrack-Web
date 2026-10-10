export interface IBillStatementStockLine {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  productName: string;
  unitName: string;
  billType: string;
  rate: number;
  openingStock: number;
  bookedQty: number;
  deliveredQty: number;
  closingStock: number;
  billingDates: string[];
  cyclesInPeriod: number;
  rentThisPeriod: number;
}

export interface IBillStatementBookingLine {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  productName: string;
  unitName: string;
  quantity: number;
  billType: string;
  rate: number;
  rentAmount: number;
  labourCharge: number;
  total: number;
}

export interface IBillStatementDeliveryLine {
  deliveryId: string;
  deliveryNumber: string;
  deliveryDate: string;
  bookingNumber: string;
  productName: string;
  unitName: string;
  quantity: number;
  billingCycles: number;
  rate: number;
  rentCharge: number;
  labourCharge: number;
  total: number;
  collectedAmount: number;
  dueAmount: number;
}

export interface IBillStatementPaymentLine {
  transactionId: string;
  deliveryId?: string;
  deliveryNumber?: string;
  date: string;
  transactionCode: string;
  paymentMethod?: string;
  description?: string;
  amount: number;
}

export interface ICustomerBillStatement {
  customerId: number;
  customerName: string;
  customerMobile?: string;
  address?: string;
  fromDate: string;
  toDate: string;

  stockLines: IBillStatementStockLine[];
  bookings: IBillStatementBookingLine[];
  deliveries: IBillStatementDeliveryLine[];
  payments: IBillStatementPaymentLine[];
  discounts: IBillStatementPaymentLine[];
  /** Charge-ledger rows dated in the period (the log of every charge on this bill). */
  chargeEntries: IChargeLedgerEntry[];

  bookingRentTotal: number;
  bookingLabourTotal: number;
  deliveryRentTotal: number;
  deliveryLabourTotal: number;
  deliveryAdjustmentTotal: number;
  deliveryCollectedTotal: number;

  previousDue: number;
  rentCharged: number;
  labourCharged: number;
  totalCharged: number;
  totalPaid: number;
  paidAtDelivery: number;
  otherPayments: number;
  totalDiscount: number;
  closingDue: number;
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
