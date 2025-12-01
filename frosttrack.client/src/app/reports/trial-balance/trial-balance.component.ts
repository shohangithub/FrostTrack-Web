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
  reportForm: UntypedFormGroup;
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

    // Initialize form with default date range (current month)
    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.reportForm = this.fb.group({
      startDate: [
        firstDayOfMonth.toISOString().split('T')[0],
        Validators.required,
      ],
      endDate: [today.toISOString().split('T')[0], Validators.required],
    });
  }

  onSubmit(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.loadTrialBalance();
  }

  loadTrialBalance(): void {
    this.isLoading = true;
    const formValue = this.reportForm.value;

    const startDate = new Date(formValue.startDate).toISOString();
    const endDate = new Date(formValue.endDate).toISOString();

    this.trialBalanceService.getTrialBalance(startDate, endDate).subscribe({
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

  print(): void {
    window.print();
  }

  resetReport(): void {
    this.showReport = false;
    this.trialBalanceData = null;
  }

  getFormattedDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  getPercentage(amount: number, total: number): number {
    if (total === 0) return 0;
    return (amount / total) * 100;
  }
}
