export interface IBookingWithDueResponse {
  bookingId: string;
  bookingNumber: string;
  bookingDate: Date;
  customerId: number;
  customerName: string;
  lastDeliveryDate: Date | null;
  totalAmount: number;
  paidAmount: number;
  dueAmount: number;
}

export interface IBookingLookupWithDue {
  value: string;
  text: string;
}
