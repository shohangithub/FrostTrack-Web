import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
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
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { todayInputFormat } from 'app/utils/date-utils';
import { DatewiseActivityReportService } from '../../services/datewise-activity-report.service';
import {
  ActivityReportType,
  IDatewiseActivityReport,
} from '../../models/datewise-activity-report.interface';

const TITLES: Record<ActivityReportType, { bn: string; en: string }> = {
  booking: { bn: 'বুকিং রিপোর্ট', en: 'Booking Report' },
  delivery: { bn: 'ডেলিভারি রিপোর্ট', en: 'Delivery Report' },
  both: { bn: 'বুকিং ও ডেলিভারি রিপোর্ট', en: 'Booking & Delivery Report' },
};

/**
 * Date-wise Booking, Delivery and Booking & Delivery reports (mode comes from route data).
 * Layout follows the General Ledger report: Daily / Date Range tabs, day-grouped tables, grand totals.
 */
@Component({
  selector: 'app-datewise-activity-report',
  templateUrl: './datewise-activity-report.component.html',
  styleUrls: [
    '../general-ledger/general-ledger.component.scss',
    './datewise-activity-report.component.scss',
  ],
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
export class DatewiseActivityReportComponent implements OnInit {
  mode: ActivityReportType = 'both';
  activeTab: 'daily' | 'range' = 'daily';

  dailyForm: UntypedFormGroup;
  rangeForm: UntypedFormGroup;

  customers: ICustomerListResponse[] = [];
  report: IDatewiseActivityReport | null = null;

  isLoading = false;
  showReport = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private route: ActivatedRoute,
    private reportService: DatewiseActivityReportService,
    private customerService: CustomerService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    const todayStr = todayInputFormat();
    this.dailyForm = this.fb.group({
      reportDate: [todayStr, Validators.required],
      customerId: [null],
    });
    this.rangeForm = this.fb.group({
      fromDate: [this.firstDayOfMonth(), Validators.required],
      toDate: [todayStr, Validators.required],
      customerId: [null],
    });
  }

  ngOnInit(): void {
    this.route.data.subscribe((data) => {
      this.mode = (data['mode'] as ActivityReportType) || 'both';
      this.report = null;
      this.showReport = false;
    });

    this.customerService.getList().subscribe({
      next: (data) => (this.customers = data),
      error: () => this.toastr.error('Failed to load customers', 'Error'),
    });
  }

  get title() {
    return TITLES[this.mode];
  }

  get showBookings(): boolean {
    return this.mode !== 'delivery';
  }

  get showDeliveries(): boolean {
    return this.mode !== 'booking';
  }

  switchTab(tab: 'daily' | 'range'): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.showReport = false;
    this.report = null;
  }

  setQuickRange(type: 'today' | 'week' | 'month' | 'lastMonth'): void {
    const now = new Date();
    const todayStr = todayInputFormat();

    if (type === 'today') {
      this.rangeForm.patchValue({ fromDate: todayStr, toDate: todayStr });
    } else if (type === 'week') {
      const startOfWeek = new Date(now);
      const diff = (startOfWeek.getDay() + 1) % 7; // week starts on Saturday
      startOfWeek.setDate(now.getDate() - diff);
      this.rangeForm.patchValue({ fromDate: this.formatDate(startOfWeek), toDate: todayStr });
    } else if (type === 'month') {
      this.rangeForm.patchValue({ fromDate: this.firstDayOfMonth(), toDate: todayStr });
    } else if (type === 'lastMonth') {
      const first = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const last = new Date(now.getFullYear(), now.getMonth(), 0);
      this.rangeForm.patchValue({ fromDate: this.formatDate(first), toDate: this.formatDate(last) });
    }
  }

  generateDailyReport(): void {
    if (this.dailyForm.invalid) {
      this.toastr.error('Please select a report date', 'Error');
      return;
    }
    const { reportDate, customerId } = this.dailyForm.value;
    this.load(reportDate, reportDate, customerId);
  }

  generateRangeReport(): void {
    if (this.rangeForm.invalid) {
      this.toastr.error('Please select valid from and to dates', 'Error');
      return;
    }
    const { fromDate, toDate, customerId } = this.rangeForm.value;
    if (fromDate > toDate) {
      this.toastr.error('From date cannot be after To date', 'Error');
      return;
    }
    this.load(fromDate, toDate, customerId);
  }

  private load(fromDate: string, toDate: string, customerId: number | null): void {
    this.isLoading = true;
    this.reportService.getReport(this.mode, fromDate, toDate, customerId).subscribe({
      next: (data) => {
        this.report = data;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(err?.error?.message || `Failed to load ${this.title.en.toLowerCase()}`, 'Error');
        this.isLoading = false;
      },
    });
  }

  resetDaily(): void {
    this.dailyForm.patchValue({ reportDate: todayInputFormat(), customerId: null });
    this.report = null;
    this.showReport = false;
  }

  resetRange(): void {
    this.rangeForm.patchValue({ fromDate: this.firstDayOfMonth(), toDate: todayInputFormat(), customerId: null });
    this.report = null;
    this.showReport = false;
  }

  /** "yyyy-MM-dd" -> local Date (avoids the UTC shift of new Date('yyyy-MM-dd')). */
  toLocalDate(value: string): Date {
    const [y, m, d] = value.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private firstDayOfMonth(): string {
    const d = new Date();
    return `${d.getFullYear()}-${(d.getMonth() + 1).toString().padStart(2, '0')}-01`;
  }

  private formatDate(date: Date): string {
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
