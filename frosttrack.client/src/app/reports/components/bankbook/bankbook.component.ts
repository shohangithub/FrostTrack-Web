import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { BankBookService } from '../../services/bankbook.service';
import { BankService } from 'app/common/services/bank.service';
import { IBankBookReport, IBankBookRangeReport } from '../../models/bankbook.interface';
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
    NgSelectModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class BankbookComponent implements OnInit {
  activeTab: 'daily' | 'range' = 'daily';

  dailyForm: UntypedFormGroup;
  rangeForm: UntypedFormGroup;

  bankBookReport: IBankBookReport | null = null;
  bankBookRangeReport: IBankBookRangeReport | null = null;
  banks: any[] = [];

  isLoading = false;
  showReport = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private bankBookService: BankBookService,
    private bankService: BankService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    const todayStr = todayInputFormat();
    this.dailyForm = this.fb.group({
      reportDate: [todayStr, Validators.required],
      bankId: [null],
    });

    this.rangeForm = this.fb.group({
      fromDate: [this.firstDayOfMonth(), Validators.required],
      toDate: [todayStr, Validators.required],
      bankId: [null],
    });
  }

  ngOnInit(): void {
    this.loadBanks();
  }

  loadBanks(): void {
    this.bankService.getLookup().subscribe({
      next: (data: any) => {
        this.banks = Array.isArray(data) ? data : data?.data || [];
      },
      error: (err) => console.error('Failed to load banks:', err),
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
      this.toastr.error('Please fill in all required fields', 'Error');
      return;
    }

    this.isLoading = true;
    const formValue = this.dailyForm.value;
    const reportDate = new Date(formValue.reportDate);
    const bankId = formValue.bankId ? Number(formValue.bankId) : null;

    this.bankBookService.getBankBook(reportDate, bankId).subscribe({
      next: (data: IBankBookReport) => {
        this.bankBookReport = data;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load bank book report', 'Error');
        console.error(error);
        this.isLoading = false;
      },
    });
  }

  generateRangeReport(): void {
    if (this.rangeForm.invalid) {
      this.toastr.error('Please select valid from and to dates', 'Error');
      return;
    }

    const formValue = this.rangeForm.value;
    const fromDate = new Date(formValue.fromDate);
    const toDate = new Date(formValue.toDate);

    if (fromDate > toDate) {
      this.toastr.error('From date cannot be after To date', 'Error');
      return;
    }

    this.isLoading = true;
    const bankId = formValue.bankId ? Number(formValue.bankId) : null;

    this.bankBookService.getBankBookRange(fromDate, toDate, bankId).subscribe({
      next: (data: IBankBookRangeReport) => {
        this.bankBookRangeReport = data;
        this.showReport = true;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error(error?.error?.message || 'Failed to load bank book range report', 'Error');
        console.error(error);
        this.isLoading = false;
      },
    });
  }

  resetDaily(): void {
    this.dailyForm.patchValue({
      reportDate: todayInputFormat(),
      bankId: null,
    });
    this.bankBookReport = null;
    this.showReport = false;
  }

  resetRange(): void {
    this.rangeForm.patchValue({
      fromDate: this.firstDayOfMonth(),
      toDate: todayInputFormat(),
      bankId: null,
    });
    this.bankBookRangeReport = null;
    this.showReport = false;
  }
}
