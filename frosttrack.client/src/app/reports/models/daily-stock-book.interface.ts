export interface IDailyStockBookItem {
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
