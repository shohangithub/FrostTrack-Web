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
import { BankBookService } from '../../services/bankbook.service';
import { IBankBookReport } from '../../models/bankbook.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-bankbook',
  templateUrl: './bankbook.component.html',
  styleUrls: ['./bankbook.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class BankbookComponent implements OnInit {
  reportForm: UntypedFormGroup;
  bankBookReport: IBankBookReport | null = null;
  isLoading = false;
  showReport = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private bankBookService: BankBookService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    this.reportForm = this.fb.group({
      reportDate: [todayInputFormat(), Validators.required],
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

    this.bankBookService.getBankBook(reportDate).subscribe({
      next: (data: IBankBookReport) => {
        this.bankBookReport = data;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load bank book report', 'Error');
        console.error('Error loading bank book:', error);
        this.isLoading = false;
      },
    });
  }

  reset(): void {
    this.reportForm.patchValue({
      reportDate: todayInputFormat(),
    });

    this.bankBookReport = null;
    this.showReport = false;
  }
}
