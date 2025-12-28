import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ILedgerBookResponse } from '../../interfaces/ledger-book.interface';
import { LedgerBookService } from '../../services/ledger-book.service';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-ledger-book',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
  templateUrl: './ledger-book.component.html',
  styleUrl: './ledger-book.component.scss',
})
export class LedgerBookComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ledgerBookService = inject(LedgerBookService);
  private readonly toastr = inject(ToastrService);

  reportForm!: FormGroup;
  ledgerBookReport: ILedgerBookResponse | null = null;
  isLoading = false;
  showReport = false;
  today = new Date();

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm(): void {
    this.reportForm = this.fb.group({
      reportDate: [todayInputFormat(), Validators.required],
    });
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const reportDate = new Date(this.reportForm.value.reportDate);

    this.ledgerBookService.getGeneralLedger(reportDate).subscribe({
      next: (response: ILedgerBookResponse) => {
        this.ledgerBookReport = response;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load ledger book report', 'Error');
        console.error('Error generating report:', error);
        this.isLoading = false;
      },
    });
  }

  reset(): void {
    this.reportForm.reset({
      reportDate: todayInputFormat(),
    });
    this.ledgerBookReport = null;
    this.showReport = false;
  }
}
