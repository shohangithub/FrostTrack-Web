export interface IDailyStockBookItem {
  bookingId?: string;
  bookingDate?: string;
  customerId: number;
  customerName: string;
  productId: number;
  productName: string;
  previousStock: number;
  totalBooking: number;
  totalDelivery: number;
  receiptNo: string;
  currentStock: number;
  receivedRent: number;
}
