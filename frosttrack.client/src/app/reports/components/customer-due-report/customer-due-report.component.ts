import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
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
import { CustomerDueReportService } from '../../services/customer-due-report.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { ICustomerDueSummaryResponse } from 'app/booking/models/booking.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-customer-due-report',
  templateUrl: './customer-due-report.component.html',
  styleUrls: ['./customer-due-report.component.scss'],
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
export class CustomerDueReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  reportItems: ICustomerDueSummaryResponse[] = [];
  customers: ICustomerListResponse[] = [];
  isLoading = false;
  showReport = false;
  today = new Date();

  statusList = [
    { id: 'all', label: 'All Status' },
    { id: 'normal', label: 'Current' },
    { id: 'warning', label: 'Due Soon (25+ days)' },
    { id: 'danger', label: 'Overdue (30+ days)' },
  ];

  dueFilterList = [
    { value: true, label: 'Outstanding Due Only' },
    { value: false, label: 'All Customers' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private customerDueReportService: CustomerDueReportService,
    private customerService: CustomerService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    this.reportForm = this.fb.group({
      reportDate: [todayInputFormat(), Validators.required],
      customerId: [null],
      status: ['all'],
      dueOnly: [true],
      searchText: [''],
    });
  }

  ngOnInit(): void {
    this.loadCustomers();
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
    if (!customerId) return 'All Customers';
    const customer = this.customers.find((c) => c.id === customerId);
    return customer ? customer.customerName : 'All Customers';
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      this.toastr.error('Please fill in all required fields', 'Error');
      return;
    }

    this.isLoading = true;
    const formValue = this.reportForm.value;
    const reportDate = new Date(formValue.reportDate);

    this.customerDueReportService
      .getCustomerDueSummary(
        reportDate,
        formValue.customerId,
        formValue.status,
        formValue.dueOnly,
        formValue.searchText,
      )
      .subscribe({
        next: (data: ICustomerDueSummaryResponse[]) => {
          this.reportItems = data || [];
          this.showReport = true;
          this.isLoading = false;
        },
        error: (error: any) => {
          this.toastr.error('Failed to load customer due report', 'Error');
          console.error('Error loading customer due report:', error);
          this.isLoading = false;
        },
      });
  }

  reset(): void {
    this.reportForm.patchValue({
      reportDate: todayInputFormat(),
      customerId: null,
      status: 'all',
      dueOnly: true,
      searchText: '',
    });

    this.generateReport();
  }

  // ── Calculation helpers ───────────────────────────────────────────────────

  getTotalCustomers(): number {
    return this.reportItems.length;
  }

  getTotalOverdue(): number {
    return this.reportItems.filter((x) => x.status === 'danger').length;
  }

  getTotalBilled(): number {
    return this.reportItems.reduce((sum, item) => sum + item.totalAmount, 0);
  }

  getTotalPaid(): number {
    return this.reportItems.reduce((sum, item) => sum + item.totalPaid, 0);
  }

  getTotalDue(): number {
    return this.reportItems.reduce((sum, item) => sum + item.totalDue, 0);
  }

  getTotalPendingRecurring(): number {
    return this.reportItems.reduce(
      (sum, item) => sum + (item.pendingRecurringChargeAmount ?? 0),
      0,
    );
  }

  getTotalLastPayment(): number {
    return this.reportItems.reduce(
      (sum, item) => sum + (item.lastPaymentAmount ?? 0),
      0,
    );
  }

  // ── Status presentation helpers ──────────────────────────────────────────

  getStatusClass(status: string): string {
    switch (status) {
      case 'danger':
        return 'badge bg-danger';
      case 'warning':
        return 'badge bg-warning text-dark';
      default:
        return 'badge bg-success';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'danger':
        return 'Overdue';
      case 'warning':
        return 'Due Soon';
      default:
        return 'Current';
    }
  }

  // ── Export CSV ────────────────────────────────────────────────────────────

  exportCsv(): void {
    if (this.reportItems.length === 0) {
      this.toastr.warning('No data to export', 'Warning');
      return;
    }

    const headers = [
      'SL',
      'Customer Name',
      'Mobile',
      'Address',
      'Bookings',
      'Total Billed',
      'Total Paid',
      'Total Due',
      'Last Payment',
      'Last Payment Date',
      'Days Since Last Payment',
      'Status',
    ];

    const rows = this.reportItems.map((r, i) => [
      i + 1,
      r.customerName,
      r.customerMobile,
      r.customerAddress || '',
      r.totalBookings,
      r.totalAmount.toFixed(2),
      r.totalPaid.toFixed(2),
      r.totalDue.toFixed(2),
      r.lastPaymentAmount !== null && r.lastPaymentAmount !== undefined
        ? r.lastPaymentAmount.toFixed(2)
        : '',
      r.lastPaymentDate ? r.lastPaymentDate.substring(0, 10) : 'No Payments',
      r.daysSinceLastPayment,
      this.getStatusText(r.status),
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
    a.download = `customer-due-report-${new Date().toISOString().substring(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
