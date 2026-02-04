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
import { DatewiseDeliveryReportService } from '../../services/datewise-delivery-report.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ProductService } from 'app/administration/services/product.service';
import { IDatewiseDeliveryReportItem } from '../../models/datewise-delivery-report.interface';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-datewise-delivery-report',
  templateUrl: './datewise-delivery-report.component.html',
  styleUrls: ['./datewise-delivery-report.component.scss'],
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
export class DatewiseDeliveryReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  deliveryReportItems: IDatewiseDeliveryReportItem[] = [];
  isLoading = false;
  showReport = false;
  fromDate = new Date();
  toDate = new Date();
  today = new Date();

  customers: ICustomerListResponse[] = [];
  products: IProductListResponse[] = [];

  constructor(
    private fb: UntypedFormBuilder,
    private datewiseDeliveryReportService: DatewiseDeliveryReportService,
    private customerService: CustomerService,
    private productService: ProductService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    // Initialize form with today's date
    this.reportForm = this.fb.group({
      fromDate: [todayInputFormat(), Validators.required],
      toDate: [todayInputFormat(), Validators.required],
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
    this.fromDate = new Date(formValue.fromDate);
    this.toDate = new Date(formValue.toDate);

    this.datewiseDeliveryReportService
      .getDatewiseDeliveryReport(
        this.fromDate,
        this.toDate,
        formValue.customerId,
        formValue.productId,
      )
      .subscribe({
        next: (data) => {
          this.deliveryReportItems = data;
          this.showReport = true;
          this.isLoading = false;

          if (this.deliveryReportItems.length === 0) {
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
      fromDate: todayInputFormat(),
      toDate: todayInputFormat(),
      customerId: null,
      productId: null,
    });
    this.showReport = false;
    this.deliveryReportItems = [];
  }

  getTotalQuantity(): number {
    return this.deliveryReportItems.reduce(
      (sum, item) => sum + item.deliveryQuantity,
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
}
