import { Component, OnInit, ViewChild } from '@angular/core';
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
  DashboardPeriod,
} from '../models/dashboard.interface';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { NgScrollbar } from 'ngx-scrollbar';

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
    RouterLink,
    NgbProgressbar,
    NgApexchartsModule,
    StockChartComponent,
    NgScrollbar,
  ],
})
export class MainComponent implements OnInit {
  public lineChartOptions!: Partial<ChartOptions>;
  public lineChartOptions2!: Partial<ChartOptions2>;
  public barChartOptions!: Partial<ChartOptions>;
  public stackBarChart!: Partial<ChartOptions>;

  // Dashboard data
  dashboardStats: IDashboardStatsResponse | null = null;
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
    this.chart1();
    this.chart2();
    this.chart3();
    this.loadDashboardData();
  }

  loadDashboardData(period?: DashboardPeriod): void {
    if (period) {
      this.selectedPeriod = period;
    }

    this.isLoading = true;
    this.dashboardService.getDashboardStats(this.selectedPeriod).subscribe({
      next: (response: IDashboardStatsResponse) => {
        this.dashboardStats = response;
        this.calculateCardData();
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load dashboard data');
        this.isLoading = false;
      },
    });
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
    this.lineChartOptions = {
      series: [
        {
          name: 'Data 1',
          data: [80, 250, 30, 120, 260, 100, 180],
        },
        {
          name: 'Data 2',
          data: [85, 130, 85, 225, 80, 190, 120],
        },
        {
          name: 'Data 3',
          data: [85, 100, 85, 305, 80, 190, 90],
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
      colors: ['#6777EF', '#8B8697'],
      stroke: {
        curve: 'smooth',
      },
      grid: {
        row: {
          colors: ['transparent', 'transparent'], // takes an array which will be repeated on columns
          opacity: 0.5,
        },
        borderColor: '#9aa0ac',
      },
      fill: {
        type: 'gradient',
        gradient: {
          gradientToColors: ['#54CA68', '#EF447C'],
          stops: [0, 50, 65, 91],
        },
      },
      markers: {
        size: 3,
      },
      xaxis: {
        categories: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'],
      },
      yaxis: {
        // opposite: true,
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
      },
    };
  }
  private chart2() {
    this.lineChartOptions2 = {
      series: [
        {
          name: 'Income',
          type: 'area',
          data: [220, 410, 66, 324, 630, 178, 389],
        },
        {
          name: 'Sales',
          type: 'line',
          data: [26, 45, 12, 37, 68, 22, 42],
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

      labels: [
        'Sunday',
        'Monday',
        'Tuesday',
        'Wednesday',
        'Thursday',
        'Friday',
        'Saturday',
      ],
      markers: {
        size: 0,
      },
      colors: ['#999999', '#6777EF'],
      dataLabels: {
        enabled: true,
        enabledOnSeries: [1, 2],
      },
      grid: {
        borderColor: '#9aa0ac',
      },
      yaxis: [
        {
          title: {
            text: 'Income',
          },
        },
        {
          opposite: true,
          title: {
            text: 'Sales',
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
    this.stackBarChart = {
      series: [
        {
          name: 'Data 1',
          data: [44, 55, 41, 67, 22, 43],
        },
        {
          name: 'Data 2',
          data: [13, 23, 20, 8, 13, 27],
        },
        {
          name: 'Data 3',
          data: [11, 17, 15, 15, 21, 14],
        },
        {
          name: 'Data 4',
          data: [21, 7, 25, 13, 22, 8],
        },
      ],
      chart: {
        type: 'bar',
        height: 310,
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
          columnWidth: '20%',
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
        categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
      },
      legend: {
        show: false,
      },
      fill: {
        opacity: 1,
        colors: ['#F0457D', '#704DAD', '#FFC107', '#a5a5a5'],
      },
    };
  }
}
