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

export interface IDashboardTrendsResponse {
  revenueTrend: IDailyTrendData[];
  expenseTrend: IDailyTrendData[];
  netProfitTrend: IDailyTrendData[];
  bookingTrend: IDailyTrendData[];
  deliveryTrend: IDailyTrendData[];
  transactionCategoryTrends: { [key: string]: number[] };
  dateLabels: string[];
}

export interface IDailyTrendData {
  date: string;
  amount: number;
  count: number;
}

export enum DashboardPeriod {
  Today = 1,
  Last7Days = 7,
  Last15Days = 15,
  Last30Days = 30,
  Last90Days = 90,
}
