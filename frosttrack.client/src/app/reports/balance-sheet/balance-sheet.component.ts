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
import { IBalanceSheetSummary } from './balance-sheet.interfaces';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';

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
  reportForm: UntypedFormGroup;
  balanceSheetData: IBalanceSheetSummary | null = null;
  isLoading = false;
  showReport = false;
  today = new Date();
  Math = Math; // Expose Math for template usage

  constructor(
    private fb: UntypedFormBuilder,
    private balanceSheetService: BalanceSheetService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    // Initialize form with today's date
    const today = new Date();

    this.reportForm = this.fb.group({
      asOfDate: [today.toISOString().split('T')[0], Validators.required],
    });
  }

  onSubmit(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.loadBalanceSheet();
  }

  loadBalanceSheet(): void {
    this.isLoading = true;
    const formValue = this.reportForm.value;

    const asOfDate = new Date(formValue.asOfDate).toISOString();

    this.balanceSheetService.getBalanceSheet(asOfDate).subscribe({
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

  print(): void {
    window.print();
  }

  resetReport(): void {
    this.showReport = false;
    this.balanceSheetData = null;
  }

  getFormattedDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  getPercentage(amount: number, total: number): number {
    if (total === 0) return 0;
    return (amount / total) * 100;
  }
}
