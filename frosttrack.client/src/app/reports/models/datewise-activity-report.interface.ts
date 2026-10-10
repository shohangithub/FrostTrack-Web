export type ActivityReportType = 'booking' | 'delivery' | 'both';

export interface IActivityTotals {
  bookingCount: number;
  bookingRent: number;
  bookingLabour: number;
  bookingTotal: number;
  deliveryCount: number;
  deliveryRent: number;
  deliveryLabour: number;
  deliveryTotal: number;
  deliveryCollected: number;
  deliveryDue: number;
}

export interface IActivityBookingLine {
  bookingId: string;
  bookingNumber: string;
  bookingDate: string;
  customerId: number;
  customerName: string;
  customerMobile?: string;
  productName: string;
  quantity: number;
  unitName: string;
  rate: number;
  rentAmount: number;
  labourCharge: number;
  total: number;
}

export interface IActivityDeliveryLine {
  deliveryId: string;
  deliveryNumber: string;
  deliveryDate: string;
  bookingNumber: string;
  customerId: number;
  customerName: string;
  customerMobile?: string;
  productName: string;
  quantity: number;
  unitName: string;
  billingCycles: number;
  rentCharge: number;
  labourCharge: number;
  total: number;
  collectedAmount: number;
  dueAmount: number;
}

export interface IActivityDay {
  date: string; // yyyy-MM-dd
  bookings: IActivityBookingLine[];
  deliveries: IActivityDeliveryLine[];
  totals: IActivityTotals;
}

export interface IDatewiseActivityReport {
  fromDate: string;
  toDate: string;
  customerId?: number;
  customerName?: string;
  includesBookings: boolean;
  includesDeliveries: boolean;
  days: IActivityDay[];
  totals: IActivityTotals;
}
