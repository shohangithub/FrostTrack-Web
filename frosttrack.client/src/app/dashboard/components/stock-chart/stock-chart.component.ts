import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
} from '@angular/forms';
import {
  ApexNonAxisChartSeries,
  ApexChart,
  ApexLegend,
  ApexDataLabels,
  ApexPlotOptions,
  ApexResponsive,
  NgApexchartsModule,
} from 'ng-apexcharts';
import { StockReportService } from 'app/reports/services/stock-report.service';
import { IStockReportItem } from 'app/reports/models/stock-report.interface';
import { ToastrService } from 'ngx-toastr';

export type StockChartOptions = {
  series: ApexNonAxisChartSeries;
  chart: ApexChart;
  labels: string[];
  colors: string[];
  legend: ApexLegend;
  dataLabels: ApexDataLabels;
  plotOptions: ApexPlotOptions;
  responsive: ApexResponsive[];
};

@Component({
  selector: 'app-stock-chart',
  templateUrl: './stock-chart.component.html',
  styleUrls: ['./stock-chart.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgApexchartsModule],
})
export class StockChartComponent implements OnInit {
  public stockChartOptions!: Partial<StockChartOptions>;
  public stockFilterForm: UntypedFormGroup;
  public stockData: IStockReportItem[] = [];
  public isLoadingStock = false;

  constructor(
    private fb: UntypedFormBuilder,
    private stockReportService: StockReportService,
    private toastr: ToastrService
  ) {
    // Initialize stock filter form with current month dates
    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.stockFilterForm = this.fb.group({
      startDate: [firstDayOfMonth.toISOString().split('T')[0]],
      endDate: [today.toISOString().split('T')[0]],
    });
  }

  ngOnInit(): void {
    this.loadStockData();
  }

  loadStockData(): void {
    this.isLoadingStock = true;
    const formValue = this.stockFilterForm.value;

    const startDate = new Date(formValue.startDate);
    const endDate = new Date(formValue.endDate);

    this.stockReportService
      .getStockReport(startDate, endDate, undefined, undefined)
      .subscribe({
        next: (response: IStockReportItem[]) => {
          this.stockData = response;
          this.updateStockChart();
          this.isLoadingStock = false;
        },
        error: () => {
          this.isLoadingStock = false;
          this.toastr.error('Failed to load stock data');
        },
      });
  }

  updateStockChart(): void {
    const totalDelivered = this.stockData.reduce(
      (sum, item) => sum + item.deliveredQuantity,
      0
    );
    const totalRemaining = this.stockData.reduce(
      (sum, item) => sum + item.remainingQuantity,
      0
    );

    this.stockChartOptions = {
      series: [totalDelivered, totalRemaining],
      chart: {
        type: 'donut',
        width: 280,
      },
      legend: {
        show: false,
      },
      dataLabels: {
        enabled: false,
      },
      plotOptions: {
        pie: {
          donut: {
            size: '65%',
            background: 'transparent',
            labels: {
              show: true,
              name: {
                show: true,
                fontSize: '22px',
                fontWeight: 600,
              },
              value: {
                show: true,
                fontSize: '16px',
                fontWeight: 400,
                color: '#9aa0ac',
              },
              total: {
                show: true,
                showAlways: true,
                label: 'Total Stock',
                fontSize: '22px',
                fontWeight: 600,
                color: '#6777EF',
              },
            },
          },
        },
      },
      colors: ['#2AC3CB', '#FFAA00'],
      labels: ['Delivered', 'Remaining'],
      responsive: [
        {
          breakpoint: 480,
          options: {},
        },
      ],
    };
  }

  onStockFilterChange(): void {
    this.loadStockData();
  }

  getTotalBooked(): number {
    return this.stockData.reduce((sum, item) => sum + item.bookingQuantity, 0);
  }

  getTotalDelivered(): number {
    return this.stockData.reduce(
      (sum, item) => sum + item.deliveredQuantity,
      0
    );
  }

  getTotalRemaining(): number {
    return this.stockData.reduce(
      (sum, item) => sum + item.remainingQuantity,
      0
    );
  }

  getDeliveryPercentage(): number {
    const totalBooked = this.getTotalBooked();
    if (totalBooked === 0) return 0;
    return Math.round((this.getTotalDelivered() / totalBooked) * 100);
  }
}
