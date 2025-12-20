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

    // Initialize form with single report date
    const today = new Date();

    this.reportForm = this.fb.group({
      reportDate: [today.toISOString().split('T')[0], Validators.required],
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
    const reportDate = new Date(this.reportForm.value.reportDate);

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

  print(): void {
    window.print();
  }

  resetReport(): void {
    const today = new Date();
    this.reportForm.reset({
      reportDate: today.toISOString().split('T')[0],
    });
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
