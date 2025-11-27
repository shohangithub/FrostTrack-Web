export interface IStockReportItem {
  bookingId: string;
  bookingNumber: string;
  bookingDate: Date;
  customerId: number;
  customerName: string;
  productId: number;
  productName: string;
  bookingQuantity: number;
  deliveredQuantity: number;
  remainingQuantity: number;
  unitName: string;
  bookingRate: number;
  totalValue: number;
  lastDeliveryDate?: Date;
  status: 'Pending' | 'Partial' | 'Completed';
}

export interface IStockSummary {
  totalBookings: number;
  totalProducts: number;
  totalBookedQuantity: number;
  totalDeliveredQuantity: number;
  totalRemainingQuantity: number;
  totalValue: number;
}

export interface ICustomerStockReport {
  customerId: number;
  customerName: string;
  items: IStockReportItem[];
  summary: {
    totalBookedQuantity: number;
    totalDeliveredQuantity: number;
    totalRemainingQuantity: number;
    totalValue: number;
  };
}

export interface IProductStockReport {
  productId: number;
  productName: string;
  items: IStockReportItem[];
  summary: {
    totalBookedQuantity: number;
    totalDeliveredQuantity: number;
    totalRemainingQuantity: number;
    totalValue: number;
  };
}
