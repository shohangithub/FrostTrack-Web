import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import {
  ReactiveFormsModule,
  FormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { CustomerDiscountReportService } from '../../services/customer-discount-report.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { ICustomerDiscountItem } from 'app/bill-collection/services/bill-collection.service';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-customer-discount-report',
  templateUrl: './customer-discount-report.component.html',
  styleUrls: ['./customer-discount-report.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    RouterLink,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class CustomerDiscountReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  reportItems: ICustomerDiscountItem[] = [];
  customers: ICustomerListResponse[] = [];
  isLoading = false;
  showReport = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private discountReportService: CustomerDiscountReportService,
    private customerService: CustomerService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
    private route: ActivatedRoute,
  ) {
    this.layoutService.loadCurrentRoute();

    // Default From Date: 30 days ago, To Date: today
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
    const fromStr = thirtyDaysAgo.toISOString().substring(0, 10);

    this.reportForm = this.fb.group({
      fromDate: [fromStr, Validators.required],
      toDate: [todayInputFormat(), Validators.required],
      customerId: [null],
      searchText: [''],
    });
  }

  ngOnInit(): void {
    this.loadCustomers();

    // Handle query param customerId if navigated from discount page
    const qCustomerId = this.route.snapshot.queryParamMap.get('customerId');
    if (qCustomerId) {
      const cId = Number(qCustomerId);
      if (!isNaN(cId) && cId > 0) {
        this.reportForm.patchValue({ customerId: cId });
      }
    }

    this.generateReport();
  }

  loadCustomers(): void {
    this.customerService.getList().subscribe({
      next: (data: ICustomerListResponse[]) => {
        this.customers = data;
      },
      error: (error: any) => {
        console.error('Failed to load customers:', error);
      },
    });
  }

  getSelectedCustomerName(): string {
    const customerId = this.reportForm.get('customerId')?.value;
    if (!customerId) return 'All Customers (সকল গ্রাহক)';
    const customer = this.customers.find((c) => c.id === customerId);
    return customer ? customer.customerName : 'All Customers (সকল গ্রাহক)';
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      this.toastr.error('Please fill in required date fields', 'Validation Error');
      return;
    }

    this.isLoading = true;
    const formValue = this.reportForm.value;

    this.discountReportService
      .getCustomerDiscountReport(
        formValue.fromDate,
        formValue.toDate,
        formValue.customerId,
        formValue.searchText,
      )
      .subscribe({
        next: (data: ICustomerDiscountItem[]) => {
          this.reportItems = data || [];
          this.showReport = true;
          this.isLoading = false;
        },
        error: (error: any) => {
          this.toastr.error('Failed to load customer discount report', 'Error');
          console.error('Error loading discount report:', error);
          this.isLoading = false;
        },
      });
  }

  reset(): void {
    const thirtyDaysAgo = new Date();
    thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);
    const fromStr = thirtyDaysAgo.toISOString().substring(0, 10);

    this.reportForm.patchValue({
      fromDate: fromStr,
      toDate: todayInputFormat(),
      customerId: null,
      searchText: '',
    });

    this.generateReport();
  }

  // ── Calculation helpers ───────────────────────────────────────────────────

  getTotalVouchers(): number {
    return this.reportItems.length;
  }

  getTotalDueBefore(): number {
    return this.reportItems.reduce((sum, item) => sum + (item.totalDue || 0), 0);
  }

  getTotalDiscount(): number {
    return this.reportItems.reduce((sum, item) => sum + (item.discountAmount || 0), 0);
  }

  getTotalCurrentDueAfter(): number {
    return this.reportItems.reduce((sum, item) => sum + (item.currentDue || 0), 0);
  }

  // ── Export CSV ────────────────────────────────────────────────────────────

  exportCsv(): void {
    if (this.reportItems.length === 0) {
      this.toastr.warning('No data to export', 'Warning');
      return;
    }

    const headers = [
      'SL',
      'Transaction Code',
      'Date',
      'Customer Name',
      'Mobile',
      'Total Due Before',
      'Discount Amount',
      'Current Due After',
      'Discount Reason',
      'Notes',
    ];

    const rows = this.reportItems.map((r, i) => [
      i + 1,
      r.transactionCode,
      r.transactionDate ? r.transactionDate.substring(0, 10) : '',
      r.customerName,
      r.customerMobile || '',
      r.totalDue.toFixed(2),
      r.discountAmount.toFixed(2),
      r.currentDue.toFixed(2),
      r.discountReason,
      r.note || '',
    ]);

    const csvContent = [headers, ...rows]
      .map((row) =>
        row.map((v) => `"${String(v).replace(/"/g, '""')}"`).join(','),
      )
      .join('\n');

    const blob = new Blob(['\uFEFF' + csvContent], {
      type: 'text/csv;charset=utf-8;',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `customer-discount-report-${new Date().toISOString().substring(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
