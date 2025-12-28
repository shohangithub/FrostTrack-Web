import { Component, OnInit } from '@angular/core';
import {
  ApexAxisChartSeries,
  ApexChart,
  ApexXAxis,
  ApexDataLabels,
  ApexStroke,
  ApexMarkers,
  ApexYAxis,
  ApexGrid,
  ApexTitleSubtitle,
  ApexTooltip,
  ApexLegend,
  ApexFill,
  ApexPlotOptions,
  ApexResponsive,
  NgApexchartsModule,
  ApexNonAxisChartSeries,
} from 'ng-apexcharts';
import { NgbProgressbar } from '@ng-bootstrap/ng-bootstrap';
import { RouterLink } from '@angular/router';
import { StockChartComponent } from '../components/stock-chart/stock-chart.component';
import { DashboardService } from '../services/dashboard.service';
import {
  IDashboardStatsResponse,
  IDashboardTrendsResponse,
  DashboardPeriod,
} from '../models/dashboard.interface';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  stroke: ApexStroke;
  dataLabels: ApexDataLabels;
  markers: ApexMarkers;
  colors: string[];
  yaxis: ApexYAxis;
  grid: ApexGrid;
  legend: ApexLegend;
  tooltip: ApexTooltip;
  fill: ApexFill;
  title: ApexTitleSubtitle;
  plotOptions: ApexPlotOptions;
  responsive: ApexResponsive[];
};

export type ChartOptions2 = {
  series: ApexAxisChartSeries;
  series2: ApexNonAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  yaxis: ApexYAxis | ApexYAxis[];
  labels: string[];
  stroke: ApexStroke;
  legend: ApexLegend;
  markers: ApexMarkers;
  dataLabels: ApexDataLabels;
  colors: string[];
  fill: ApexFill;
  grid: ApexGrid;
  tooltip: ApexTooltip;
  plotOptions: ApexPlotOptions;
  responsive: ApexResponsive | ApexResponsive[];
};
@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    NgbProgressbar,
    NgApexchartsModule,
    StockChartComponent,
  ],
})
export class MainComponent implements OnInit {
  public lineChartOptions!: Partial<ChartOptions>;
  public lineChartOptions2!: Partial<ChartOptions2>;
  public barChartOptions!: Partial<ChartOptions>;
  public stackBarChart!: Partial<ChartOptions>;

  // Dashboard data
  dashboardStats: IDashboardStatsResponse | null = null;
  dashboardTrends: IDashboardTrendsResponse | null = null;
  isLoading = false;
  selectedPeriod: DashboardPeriod = DashboardPeriod.Last30Days;
  DashboardPeriod = DashboardPeriod; // Expose enum to template
  Math = Math; // Expose Math for template

  // Card data
  bookingData = {
    count: 0,
    amount: 0,
    percentage: 0,
  };

  deliveryData = {
    count: 0,
    amount: 0,
    percentage: 0,
  };

  billCollectionData = {
    count: 0,
    amount: 0,
    percentage: 0,
  };

  revenueData = {
    revenue: 0,
    expense: 0,
    net: 0,
    percentage: 0,
  };

  constructor(
    private dashboardService: DashboardService,
    private toastr: ToastrService
  ) {}

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData(period?: DashboardPeriod): void {
    if (period) {
      this.selectedPeriod = period;
    }

    this.isLoading = true;

    // Load both stats and trends
    Promise.all([
      this.dashboardService.getDashboardStats(this.selectedPeriod).toPromise(),
      this.dashboardService.getDashboardTrends(this.selectedPeriod).toPromise(),
    ])
      .then(([stats, trends]) => {
        this.dashboardStats = stats!;
        this.dashboardTrends = trends!;
        this.calculateCardData();
        this.updateCharts();
        this.isLoading = false;
      })
      .catch(() => {
        this.toastr.error('Failed to load dashboard data');
        this.isLoading = false;
      });
  }

  /**
   * Handle period change from dropdown
   */
  onPeriodChange(): void {
    this.loadDashboardData();
  }

  updateCharts(): void {
    if (!this.dashboardTrends) return;
    this.chart1();
    this.chart2();
    this.chart3();
  }

  calculateCardData(): void {
    if (!this.dashboardStats) return;

    const maxTarget = 1000; // Target values for percentage calculation

    // Booking card
    this.bookingData = {
      count: this.dashboardStats.totalBookings,
      amount: this.dashboardStats.totalBookingAmount,
      percentage: Math.min(
        (this.dashboardStats.totalBookings / maxTarget) * 100,
        100
      ),
    };

    // Delivery card
    this.deliveryData = {
      count: this.dashboardStats.totalDeliveries,
      amount: this.dashboardStats.totalDeliveryAmount,
      percentage: Math.min(
        (this.dashboardStats.totalDeliveries / maxTarget) * 100,
        100
      ),
    };

    // Bill Collection card
    this.billCollectionData = {
      count: this.dashboardStats.totalBillCollections,
      amount: this.dashboardStats.totalBillCollectionAmount,
      percentage: Math.min(
        (this.dashboardStats.totalBillCollections / maxTarget) * 100,
        100
      ),
    };

    // Revenue card
    const revenueTarget = 100000;
    this.revenueData = {
      revenue: this.dashboardStats.totalRevenue,
      expense: this.dashboardStats.totalExpense,
      net: this.dashboardStats.netRevenue,
      percentage: Math.min(
        (this.dashboardStats.netRevenue / revenueTarget) * 100,
        100
      ),
    };
  }

  getPeriodLabel(period: DashboardPeriod): string {
    switch (period) {
      case DashboardPeriod.Last7Days:
        return 'Last 7 Days';
      case DashboardPeriod.Last15Days:
        return 'Last 15 Days';
      case DashboardPeriod.Last30Days:
        return 'Last 30 Days';
      case DashboardPeriod.Last90Days:
        return 'Last 90 Days';
      default:
        return 'Last 30 Days';
    }
  }
  private chart1() {
    if (!this.dashboardTrends) return;

    const revenueData = this.dashboardTrends.revenueTrend.map((d) => d.amount);
    const expenseData = this.dashboardTrends.expenseTrend.map((d) => d.amount);
    const netProfitData = this.dashboardTrends.netProfitTrend.map(
      (d) => d.amount
    );

    // Get categories from trend dates or use date labels
    const categories =
      this.dashboardTrends.dateLabels.length > 0
        ? this.dashboardTrends.dateLabels
        : this.dashboardTrends.revenueTrend.map((d) =>
            new Date(d.date).toLocaleDateString('en-US', {
              month: 'short',
              day: 'numeric',
            })
          );

    this.lineChartOptions = {
      series: [
        {
          name: 'Revenue',
          data: revenueData,
        },
        {
          name: 'Expenses',
          data: expenseData,
        },
        {
          name: 'Net Profit',
          data: netProfitData,
        },
      ],
      chart: {
        height: 350,
        type: 'line',
        foreColor: '#9aa0ac',
        dropShadow: {
          enabled: true,
          color: '#000',
          top: 18,
          left: 7,
          blur: 10,
          opacity: 0.2,
        },
        toolbar: {
          show: false,
        },
      },
      colors: ['#54CA68', '#EF447C', '#6777EF'],
      stroke: {
        curve: 'smooth',
        width: 3,
      },
      grid: {
        row: {
          colors: ['transparent', 'transparent'],
          opacity: 0.5,
        },
        borderColor: '#9aa0ac',
      },
      fill: {
        type: 'gradient',
        gradient: {
          gradientToColors: ['#54CA68', '#EF447C', '#6777EF'],
          stops: [0, 50, 100],
        },
      },
      markers: {
        size: 3,
      },
      xaxis: {
        categories: categories,
      },
      yaxis: {
        labels: {
          formatter: function (val) {
            return '৳' + val.toFixed(0);
          },
        },
      },
      legend: {
        position: 'top',
        horizontalAlign: 'right',
        floating: true,
        offsetY: -25,
        offsetX: -5,
      },
      tooltip: {
        theme: 'dark',
        marker: {
          show: true,
        },
        x: {
          show: true,
        },
        y: {
          formatter: function (val) {
            return '৳' + val.toFixed(2);
          },
        },
      },
    };
  }
  private chart2() {
    if (!this.dashboardTrends) return;

    const bookingData = this.dashboardTrends.bookingTrend.map((d) => d.count);
    const deliveryData = this.dashboardTrends.deliveryTrend.map((d) => d.count);

    const categories =
      this.dashboardTrends.dateLabels.length > 0
        ? this.dashboardTrends.dateLabels
        : this.dashboardTrends.bookingTrend.map((d) =>
            new Date(d.date).toLocaleDateString('en-US', {
              month: 'short',
              day: 'numeric',
            })
          );

    this.lineChartOptions2 = {
      series: [
        {
          name: 'Bookings',
          type: 'area',
          data: bookingData,
        },
        {
          name: 'Deliveries',
          type: 'line',
          data: deliveryData,
        },
      ],
      chart: {
        height: 350,
        type: 'area',
        foreColor: '#9aa0ac',
        toolbar: {
          show: false,
        },
      },
      fill: {
        type: 'solid',
        opacity: [0.35, 1],
      },
      stroke: {
        width: [0, 4],
        curve: 'smooth',
      },
      labels: categories,
      markers: {
        size: 0,
      },
      colors: ['#6777EF', '#54CA68'],
      dataLabels: {
        enabled: false,
      },
      grid: {
        borderColor: '#9aa0ac',
      },
      yaxis: [
        {
          title: {
            text: 'Bookings',
          },
        },
        {
          opposite: true,
          title: {
            text: 'Deliveries',
          },
        },
      ],
      xaxis: {
        labels: {
          trim: false,
        },
      },
      tooltip: {
        theme: 'dark',
        marker: {
          show: true,
        },
        x: {
          show: true,
        },
      },
    };
  }

  private chart3() {
    if (!this.dashboardTrends) return;

    const categories =
      this.dashboardTrends.dateLabels.length > 0
        ? this.dashboardTrends.dateLabels
        : [];

    const categoryTrends = this.dashboardTrends.transactionCategoryTrends;

    this.stackBarChart = {
      series: [
        {
          name: 'Bill Collection',
          data: categoryTrends['BILL_COLLECTION'] || [],
        },
        {
          name: 'Bill Payment',
          data: categoryTrends['BILL_PAYMENT'] || [],
        },
        {
          name: 'Salary',
          data: categoryTrends['SALARY'] || [],
        },
        {
          name: 'Office Cost',
          data: categoryTrends['OFFICE_COST'] || [],
        },
      ],
      chart: {
        type: 'bar',
        height: 350,
        foreColor: '#9aa0ac',
        stacked: true,
        toolbar: {
          show: false,
        },
      },
      responsive: [
        {
          breakpoint: 480,
          options: {
            legend: {
              position: 'bottom',
              offsetX: -10,
              offsetY: 0,
            },
          },
        },
      ],
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '40%',
        },
      },
      dataLabels: {
        enabled: false,
      },
      grid: {
        borderColor: '#9aa0ac',
      },
      xaxis: {
        type: 'category',
        categories: categories,
      },
      legend: {
        position: 'top',
        horizontalAlign: 'left',
      },
      fill: {
        opacity: 1,
        colors: ['#54CA68', '#EF447C', '#FFC107', '#6777EF'],
      },
      yaxis: {
        labels: {
          formatter: function (val) {
            return '৳' + val.toFixed(0);
          },
        },
      },
      tooltip: {
        theme: 'dark',
        y: {
          formatter: function (val) {
            return '৳' + val.toFixed(2);
          },
        },
      },
    };
  }
}
