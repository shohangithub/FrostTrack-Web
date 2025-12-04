export interface IDashboardStatsResponse {
  totalBookings: number;
  totalBookingAmount: number;
  totalDeliveries: number;
  totalDeliveryAmount: number;
  totalBillCollections: number;
  totalBillCollectionAmount: number;
  totalRevenue: number;
  totalExpense: number;
  netRevenue: number;
  startDate: string;
  endDate: string;
  periodDays: number;
}

export interface IDashboardCardData {
  title: string;
  value: string;
  subValue: string;
  progressPercentage: number;
  progressType: 'success' | 'warning' | 'info' | 'danger';
}

export enum DashboardPeriod {
  Last7Days = 7,
  Last15Days = 15,
  Last30Days = 30,
  Last90Days = 90,
}
