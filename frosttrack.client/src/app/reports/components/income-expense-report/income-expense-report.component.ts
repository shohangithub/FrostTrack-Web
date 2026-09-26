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
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { dateInputFormat, todayInputFormat } from 'app/utils/date-utils';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { ITransactionListResponse } from 'app/transaction/models/transaction.interface';

export interface IHeadSummary {
  headName: string;
  count: number;
  total: number;
}

@Component({
  selector: 'app-income-expense-report',
  templateUrl: './income-expense-report.component.html',
  styleUrls: ['./income-expense-report.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class IncomeExpenseReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  isLoading = false;
  showReport = false;
  today = new Date();

  incomeHeads: IHeadSummary[] = [];
  expenseHeads: IHeadSummary[] = [];

  totalIncome = 0;
  totalExpense = 0;
  netBalance = 0;

  // Full transactions breakdown for the detail section
  allTransactions: ITransactionListResponse[] = [];

  constructor(
    private fb: UntypedFormBuilder,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    this.reportForm = this.fb.group({
      reportDate: [todayInputFormat(), Validators.required],
    });
  }

  ngOnInit(): void {}

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please select a report date', 'Error');
      return;
    }

    this.isLoading = true;
    const { reportDate } = this.reportForm.value;

    this.transactionService
      .getTransactionReport(reportDate, reportDate)
      .subscribe({
        next: (data: ITransactionListResponse[]) => {
          this.allTransactions = data || [];
          this.buildSummary(this.allTransactions);
          this.showReport = true;
          this.isLoading = false;
        },
        error: () => {
          this.toastr.error('Failed to load report data', 'Error');
          this.isLoading = false;
        },
      });
  }

  buildSummary(transactions: ITransactionListResponse[]): void {
    const incomeMap = new Map<string, IHeadSummary>();
    const expenseMap = new Map<string, IHeadSummary>();

    for (const t of transactions) {
      if (!t.netAmount || t.netAmount <= 0) continue;

      const headName = t.transactionHead?.name || 'Others';
      const displayType = (t.transactionHead?.displayType || '').toUpperCase();
      const headType = (t.transactionHead?.type || '').toUpperCase();
      const desc = (t.description || '').toUpperCase();
      const paymentMethod = (t.paymentMethod || '').toUpperCase();

      // Discount / adjustment → skip (accounting entries, not cash)
      if (
        paymentMethod === 'DISCOUNT' ||
        headName.toUpperCase().includes('DISCOUNT') ||
        headName.toUpperCase().includes('ADJUSTMENT') ||
        displayType.includes('DISCOUNT') ||
        displayType.includes('ADJUSTMENT')
      ) {
        continue;
      }

      const isExpense = this.classifyAsExpense(
        headName,
        displayType,
        headType,
        desc,
        t
      );

      const targetMap = isExpense ? expenseMap : incomeMap;
      const existing = targetMap.get(headName);
      if (existing) {
        existing.count++;
        existing.total += t.netAmount;
      } else {
        targetMap.set(headName, {
          headName,
          count: 1,
          total: t.netAmount,
        });
      }
    }

    this.incomeHeads = [...incomeMap.values()].sort(
      (a, b) => b.total - a.total
    );
    this.expenseHeads = [...expenseMap.values()].sort(
      (a, b) => b.total - a.total
    );

    this.totalIncome = this.incomeHeads.reduce((s, h) => s + h.total, 0);
    this.totalExpense = this.expenseHeads.reduce((s, h) => s + h.total, 0);
    this.netBalance = this.totalIncome - this.totalExpense;
  }

  private classifyAsExpense(
    headName: string,
    displayType: string,
    headType: string,
    desc: string,
    t: ITransactionListResponse
  ): boolean {
    const hType = (headType || '').toUpperCase().trim();
    // In FrostTrack, DEBIT is Money IN (আয়/Income), CREDIT is Money OUT (ব্যয়/Expense)
    if (hType === 'DEBIT') {
      return false; // Income
    }
    if (hType === 'CREDIT') {
      return true; // Expense
    }

    const dType = (displayType || '').toUpperCase().trim();
    if (dType === 'INCOME' || dType === 'আয়') {
      return false;
    }
    if (dType === 'EXPENSE' || dType === 'ব্যয়') {
      return true;
    }

    const name = (headName || '').toUpperCase();
    const d = (desc || '').toUpperCase();
    if (
      name.includes('EXPENSE') ||
      name.includes('খরচ') ||
      name.includes('প্রদান') ||
      name.includes('বেতন') ||
      name.includes('SALARY') ||
      d.includes('EXPENSE') ||
      d.includes('খরচ') ||
      d.includes('বেতন')
    ) {
      return true;
    }

    if (Boolean(t.employeeId && !t.customerId)) return true;

    return false;
  }

  get maxRows(): number {
    return Math.max(this.incomeHeads.length, this.expenseHeads.length);
  }

  get rowIndices(): number[] {
    return Array.from({ length: this.maxRows }, (_, i) => i);
  }

  reset(): void {
    this.showReport = false;
    this.incomeHeads = [];
    this.expenseHeads = [];
    this.totalIncome = 0;
    this.totalExpense = 0;
    this.netBalance = 0;
    this.allTransactions = [];
  }
}
