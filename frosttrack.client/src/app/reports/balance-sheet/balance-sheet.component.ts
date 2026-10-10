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
import { BalanceSheetService } from './balance-sheet.service';
import { IBalanceSheetSummary, IBalanceSheetComparative } from './balance-sheet.interfaces';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-balance-sheet',
  templateUrl: './balance-sheet.component.html',
  styleUrls: ['./balance-sheet.component.scss'],
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
export class BalanceSheetComponent {
  activeTab: 'single' | 'comparative' = 'single';

  singleForm: UntypedFormGroup;
  comparativeForm: UntypedFormGroup;

  balanceSheetData: IBalanceSheetSummary | null = null;
  comparativeData: IBalanceSheetComparative | null = null;

  isLoading = false;
  showReport = false;
  today = new Date();
  Math = Math;

  constructor(
    private fb: UntypedFormBuilder,
    private balanceSheetService: BalanceSheetService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    const todayStr = todayInputFormat();
    this.singleForm = this.fb.group({
      reportDate: [todayStr, Validators.required],
    });

    const lastMonthStr = this.lastMonthSameDay();
    this.comparativeForm = this.fb.group({
      date1: [lastMonthStr, Validators.required],
      date2: [todayStr, Validators.required],
    });
  }

  lastMonthSameDay(): string {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    const year = d.getFullYear();
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  switchTab(tab: 'single' | 'comparative'): void {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.showReport = false;
  }

  generateSingleReport(): void {
    if (this.singleForm.invalid) {
      this.toastr.error('Please select a report date');
      return;
    }

    this.isLoading = true;
    const reportDate = new Date(this.singleForm.value.reportDate);

    this.balanceSheetService.getBalanceSheet(reportDate).subscribe({
      next: (response: IBalanceSheetSummary) => {
        this.balanceSheetData = response;
        this.showReport = true;
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load balance sheet');
        this.isLoading = false;
        this.showReport = false;
      },
    });
  }

  generateComparativeReport(): void {
    if (this.comparativeForm.invalid) {
      this.toastr.error('Please select both dates');
      return;
    }

    const date1 = new Date(this.comparativeForm.value.date1);
    const date2 = new Date(this.comparativeForm.value.date2);

    this.isLoading = true;
    this.balanceSheetService.getComparativeBalanceSheet(date1, date2).subscribe({
      next: (response: IBalanceSheetComparative) => {
        this.comparativeData = response;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (err: any) => {
        this.toastr.error(err?.error?.message || 'Failed to load comparative balance sheet');
        this.isLoading = false;
        this.showReport = false;
      },
    });
  }

  resetSingle(): void {
    this.singleForm.patchValue({ reportDate: todayInputFormat() });
    this.showReport = false;
    this.balanceSheetData = null;
  }

  resetComparative(): void {
    this.comparativeForm.patchValue({
      date1: this.lastMonthSameDay(),
      date2: todayInputFormat(),
    });
    this.showReport = false;
    this.comparativeData = null;
  }
}
