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
import { DailyStockBookService } from '../../services/daily-stock-book.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ProductService } from 'app/administration/services/product.service';
import { IDailyStockBookItem } from '../../models/daily-stock-book.interface';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-daily-stock-book',
  templateUrl: './daily-stock-book.component.html',
  styleUrls: ['./daily-stock-book.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgSelectModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class DailyStockBookComponent implements OnInit {
  reportForm: UntypedFormGroup;
  stockBookItems: IDailyStockBookItem[] = [];
  isLoading = false;
  showReport = false;
  reportDate = new Date();
  today = new Date();

  customers: ICustomerListResponse[] = [];
  products: IProductListResponse[] = [];

  constructor(
    private fb: UntypedFormBuilder,
    private dailyStockBookService: DailyStockBookService,
    private customerService: CustomerService,
    private productService: ProductService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    // Initialize form with today's date

    this.reportForm = this.fb.group({
      reportDate: [todayInputFormat(), Validators.required],
      customerId: [null],
      productId: [null],
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

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.showReport = false;

    const formValue = this.reportForm.value;
    this.reportDate = new Date(formValue.reportDate);

    this.dailyStockBookService
      .getDailyStockBook(
        this.reportDate,
        formValue.customerId,
        formValue.productId,
      )
      .subscribe({
        next: (data) => {
          this.stockBookItems = data;
          this.showReport = true;
          this.isLoading = false;

          if (this.stockBookItems.length === 0) {
            this.toastr.info('No data found for the selected criteria');
          }
        },
        error: (error) => {
          this.toastr.error('Failed to generate report', 'Error');
          console.error('Error generating report:', error);
          this.isLoading = false;
        },
      });
  }

  reset(): void {
    this.reportForm.reset({
      reportDate: todayInputFormat(),
      customerId: null,
      productId: null,
    });
    this.showReport = false;
    this.stockBookItems = [];
  }

  getTotalPreviousStock(): number {
    return this.stockBookItems.reduce(
      (sum, item) => sum + item.previousStock,
      0,
    );
  }

  getTotalBooking(): number {
    return this.stockBookItems.reduce(
      (sum, item) => sum + item.totalBooking,
      0,
    );
  }

  getTotalDelivery(): number {
    return this.stockBookItems.reduce(
      (sum, item) => sum + item.totalDelivery,
      0,
    );
  }

  getTotalCurrentStock(): number {
    return this.stockBookItems.reduce(
      (sum, item) => sum + item.currentStock,
      0,
    );
  }

  getTotalReceivedRent(): number {
    return this.stockBookItems.reduce(
      (sum, item) => sum + item.receivedRent,
      0,
    );
  }

  getSelectedCustomerName(): string {
    const customerId = this.reportForm.value.customerId;
    if (!customerId) return 'All Customers';
    const customer = this.customers.find((c) => c.id === customerId);
    return customer ? customer.customerName : 'All Customers';
  }

  getSelectedProductName(): string {
    const productId = this.reportForm.value.productId;
    if (!productId) return 'All Products';
    const product = this.products.find((p) => p.id === productId);
    return product ? product.productName : 'All Products';
  }

  formatReceiptNumbers(receiptNo: string): string {
    if (!receiptNo) return '';
    const receipts = receiptNo
      .split(',')
      .map((r) => r.trim())
      .filter((r) => r);
    if (receipts.length <= 2) {
      return receipts.join(', ');
    }
    const firstTwo = receipts.slice(0, 2).join(', ');
    const remaining = receipts.length - 2;
    return `${firstTwo} +${remaining}`;
  }
}
