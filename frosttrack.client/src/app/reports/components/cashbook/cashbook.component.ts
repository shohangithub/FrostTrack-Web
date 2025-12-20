import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { CashBookService } from '../../services/cashbook.service';
import { ICashBookReport } from '../../models/cashbook.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';

@Component({
  selector: 'app-cashbook',
  templateUrl: './cashbook.component.html',
  styleUrls: ['./cashbook.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class CashbookComponent implements OnInit {
  reportForm: UntypedFormGroup;
  cashBookReport: ICashBookReport | null = null;
  isLoading = false;
  showReport = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private cashBookService: CashBookService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    // Initialize form with today's date
    const today = new Date();

    this.reportForm = this.fb.group({
      reportDate: [today.toISOString().split('T')[0], Validators.required],
    });
  }

  ngOnInit(): void {
    // Component initialization
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please fill in all required fields', 'Error');
      return;
    }

    this.isLoading = true;
    const formValue = this.reportForm.value;
    const reportDate = new Date(formValue.reportDate);

    this.cashBookService.getCashBook(reportDate).subscribe({
      next: (data: ICashBookReport) => {
        this.cashBookReport = data;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load cash book report', 'Error');
        console.error('Error loading cash book:', error);
        this.isLoading = false;
      },
    });
  }

  reset(): void {
    const today = new Date();

    this.reportForm.patchValue({
      reportDate: today.toISOString().split('T')[0],
    });

    this.cashBookReport = null;
    this.showReport = false;
  }
}
