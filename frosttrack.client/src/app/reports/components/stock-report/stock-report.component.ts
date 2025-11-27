import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { StockReportService } from '../../services/stock-report.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ProductService } from 'app/administration/services/product.service';
import {
  IStockReportItem,
  IStockSummary,
} from '../../models/stock-report.interface';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { NgApexchartsModule } from 'ng-apexcharts';

@Component({
  selector: 'app-stock-report',
  templateUrl: './stock-report.component.html',
  styleUrls: ['./stock-report.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgSelectModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
    NgApexchartsModule,
  ],
})
export class StockReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  stockItems: IStockReportItem[] = [];
  filteredItems: IStockReportItem[] = [];
  summary: IStockSummary | null = null;
  isLoading = false;
  showReport = false;
  today = new Date();

  customers: ICustomerListResponse[] = [];
  products: IProductListResponse[] = [];

  reportTypeOptions = [
    { value: 'all', text: 'All Stock' },
    { value: 'pending', text: 'Pending Deliveries Only' },
    { value: 'partial', text: 'Partial Deliveries Only' },
    { value: 'completed', text: 'Completed Deliveries Only' },
  ];

  groupByOptions = [
    { value: 'none', text: 'No Grouping' },
    { value: 'customer', text: 'Group by Customer' },
    { value: 'product', text: 'Group by Product' },
  ];

  chartOptions: any;

  constructor(
    private fb: UntypedFormBuilder,
    private stockReportService: StockReportService,
    private customerService: CustomerService,
    private productService: ProductService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.reportForm = this.fb.group({
      startDate: [
        firstDayOfMonth.toISOString().split('T')[0],
        Validators.required,
      ],
      endDate: [today.toISOString().split('T')[0], Validators.required],
      customerId: [null],
      productId: [null],
      reportType: ['all'],
      groupBy: ['none'],
    });
  }

  ngOnInit(): void {
    this.loadCustomers();
    this.loadProducts();
  }

  loadCustomers(): void {
    this.customerService.getList().subscribe({
      next: (data: ICustomerListResponse[]) => {
        this.customers = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load customers', 'Error');
        console.error('Error loading customers:', error);
      },
    });
  }

  loadProducts(): void {
    this.productService.getList().subscribe({
      next: (data: IProductListResponse[]) => {
        this.products = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load products', 'Error');
        console.error('Error loading products:', error);
      },
    });
  }

  onSubmit(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.loadStockReport();
  }

  loadStockReport(): void {
    this.isLoading = true;
    const formValue = this.reportForm.value;

    const startDate = new Date(formValue.startDate);
    const endDate = new Date(formValue.endDate);

    this.stockReportService
      .getStockReport(
        startDate,
        endDate,
        formValue.customerId,
        formValue.productId
      )
      .subscribe({
        next: (response: IStockReportItem[]) => {
          this.stockItems = response;
          this.applyFilters();
          this.calculateSummary();
          this.initializeChart();
          this.showReport = true;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
          this.toastr.error('Failed to load stock report');
        },
      });
  }

  applyFilters(): void {
    const reportType = this.reportForm.value.reportType;

    this.filteredItems = this.stockItems.filter((item) => {
      if (reportType === 'pending') {
        return item.status === 'Pending';
      } else if (reportType === 'partial') {
        return item.status === 'Partial';
      } else if (reportType === 'completed') {
        return item.status === 'Completed';
      }
      return true;
    });
  }

  calculateSummary(): void {
    this.summary = {
      totalBookings: new Set(this.filteredItems.map((i) => i.bookingId)).size,
      totalProducts: new Set(this.filteredItems.map((i) => i.productId)).size,
      totalBookedQuantity: this.filteredItems.reduce(
        (sum, item) => sum + item.bookingQuantity,
        0
      ),
      totalDeliveredQuantity: this.filteredItems.reduce(
        (sum, item) => sum + item.deliveredQuantity,
        0
      ),
      totalRemainingQuantity: this.filteredItems.reduce(
        (sum, item) => sum + item.remainingQuantity,
        0
      ),
      totalValue: this.filteredItems.reduce(
        (sum, item) => sum + item.totalValue,
        0
      ),
    };
  }

  getGroupedData(): any[] {
    const groupBy = this.reportForm.value.groupBy;

    if (groupBy === 'customer') {
      const grouped = new Map<string, IStockReportItem[]>();
      this.filteredItems.forEach((item) => {
        const key = `${item.customerId}-${item.customerName}`;
        if (!grouped.has(key)) {
          grouped.set(key, []);
        }
        grouped.get(key)!.push(item);
      });

      return Array.from(grouped.entries()).map(([key, items]) => ({
        groupName: key.split('-')[1],
        items,
        subtotal: {
          bookedQty: items.reduce((sum, i) => sum + i.bookingQuantity, 0),
          deliveredQty: items.reduce((sum, i) => sum + i.deliveredQuantity, 0),
          remainingQty: items.reduce((sum, i) => sum + i.remainingQuantity, 0),
          totalValue: items.reduce((sum, i) => sum + i.totalValue, 0),
        },
      }));
    } else if (groupBy === 'product') {
      const grouped = new Map<string, IStockReportItem[]>();
      this.filteredItems.forEach((item) => {
        const key = `${item.productId}-${item.productName}`;
        if (!grouped.has(key)) {
          grouped.set(key, []);
        }
        grouped.get(key)!.push(item);
      });

      return Array.from(grouped.entries()).map(([key, items]) => ({
        groupName: key.split('-')[1],
        items,
        subtotal: {
          bookedQty: items.reduce((sum, i) => sum + i.bookingQuantity, 0),
          deliveredQty: items.reduce((sum, i) => sum + i.deliveredQuantity, 0),
          remainingQty: items.reduce((sum, i) => sum + i.remainingQuantity, 0),
          totalValue: items.reduce((sum, i) => sum + i.totalValue, 0),
        },
      }));
    }

    return [
      {
        groupName: 'All Items',
        items: this.filteredItems,
        subtotal: null,
      },
    ];
  }

  initializeChart(): void {
    const bookedQty = this.summary?.totalBookedQuantity || 0;
    const deliveredQty = this.summary?.totalDeliveredQuantity || 0;
    const remainingQty = this.summary?.totalRemainingQuantity || 0;

    this.chartOptions = {
      series: [deliveredQty, remainingQty],
      chart: {
        type: 'donut',
        height: 280,
      },
      labels: ['Delivered', 'Remaining'],
      colors: ['#28a745', '#ffc107'],
      legend: {
        position: 'bottom',
      },
      plotOptions: {
        pie: {
          donut: {
            size: '65%',
            labels: {
              show: true,
              name: {
                show: true,
                fontSize: '18px',
              },
              value: {
                show: true,
                fontSize: '24px',
                fontWeight: 600,
              },
              total: {
                show: true,
                showAlways: true,
                label: 'Total Booked',
                fontSize: '16px',
                fontWeight: 400,
                formatter: () => bookedQty.toString(),
              },
            },
          },
        },
      },
      dataLabels: {
        enabled: true,
      },
    };
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'badge-completed';
      case 'Partial':
        return 'badge-partial';
      case 'Pending':
        return 'badge-pending';
      default:
        return 'badge-default';
    }
  }

  getCustomerName(customerId: number): string {
    const customer = this.customers.find((c) => c.id === customerId);
    return customer ? customer.customerName : 'N/A';
  }

  getProductName(productId: number): string {
    const product = this.products.find((p) => p.id === productId);
    return product ? product.productName : 'N/A';
  }

  exportToCSV(): void {
    const filename = `Stock_Report_${this.reportForm.value.startDate}_to_${this.reportForm.value.endDate}`;
    this.stockReportService.exportToCSV(this.filteredItems, filename);
    this.toastr.success('Report exported successfully');
  }

  print(): void {
    window.print();
  }
}
