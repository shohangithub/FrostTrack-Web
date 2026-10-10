import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { TrialBalanceService } from './trial-balance.service';
import { ITrialBalanceSummary } from './trial-balance.interfaces';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-trial-balance',
  templateUrl: './trial-balance.component.html',
  styleUrls: ['./trial-balance.component.scss'],
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
export class TrialBalanceComponent {
  activeTab: 'daily' | 'range' = 'daily';

  dailyForm: UntypedFormGroup;
  rangeForm: UntypedFormGroup;

  trialBalanceData: ITrialBalanceSummary | null = null;
  isLoading = false;
  showReport = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private trialBalanceService: TrialBalanceService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    const todayStr = todayInputFormat();
    this.dailyForm = this.fb.group({
      reportDate: [todayStr, Validators.required],
    });

    this.rangeForm = this.fb.group({
      fromDate: [this.firstDayOfMonth(), Validators.required],
      toDate: [todayStr, Validators.required],
    });
  }

  switchTab(tab: 'daily' | 'range'): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.showReport = false;
  }

  firstDayOfMonth(): string {
    const d = new Date();
    const year = d.getFullYear();
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    return `${year}-${month}-01`;
  }

  setQuickRange(type: 'today' | 'week' | 'month' | 'lastMonth'): void {
    const now = new Date();
    const todayStr = todayInputFormat();

    if (type === 'today') {
      this.rangeForm.patchValue({ fromDate: todayStr, toDate: todayStr });
    } else if (type === 'week') {
      const startOfWeek = new Date(now);
      const day = startOfWeek.getDay();
      const diff = (day + 1) % 7;
      startOfWeek.setDate(now.getDate() - diff);
      this.rangeForm.patchValue({
        fromDate: this.formatDate(startOfWeek),
        toDate: todayStr,
      });
    } else if (type === 'month') {
      this.rangeForm.patchValue({
        fromDate: this.firstDayOfMonth(),
        toDate: todayStr,
      });
    } else if (type === 'lastMonth') {
      const first = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const last = new Date(now.getFullYear(), now.getMonth(), 0);
      this.rangeForm.patchValue({
        fromDate: this.formatDate(first),
        toDate: this.formatDate(last),
      });
    }
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  generateDailyReport(): void {
    if (this.dailyForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.isLoading = true;
    const reportDate = new Date(this.dailyForm.value.reportDate);

    this.trialBalanceService.getTrialBalance(reportDate).subscribe({
      next: (response: ITrialBalanceSummary) => {
        this.trialBalanceData = response;
        this.showReport = true;
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load trial balance report');
        this.isLoading = false;
        this.showReport = false;
      },
    });
  }

  generateRangeReport(): void {
    if (this.rangeForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    const fromDate = new Date(this.rangeForm.value.fromDate);
    const toDate = new Date(this.rangeForm.value.toDate);

    if (fromDate > toDate) {
      this.toastr.error('From date cannot be after To date');
      return;
    }

    this.isLoading = true;
    this.trialBalanceService.getTrialBalanceRange(fromDate, toDate).subscribe({
      next: (response: ITrialBalanceSummary) => {
        this.trialBalanceData = response;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (err: any) => {
        this.toastr.error(err?.error?.message || 'Failed to load trial balance range report');
        this.isLoading = false;
        this.showReport = false;
      },
    });
  }

  resetDaily(): void {
    this.dailyForm.patchValue({ reportDate: todayInputFormat() });
    this.showReport = false;
    this.trialBalanceData = null;
  }

  resetRange(): void {
    this.rangeForm.patchValue({
      fromDate: this.firstDayOfMonth(),
      toDate: todayInputFormat(),
    });
    this.showReport = false;
    this.trialBalanceData = null;
  }
}
