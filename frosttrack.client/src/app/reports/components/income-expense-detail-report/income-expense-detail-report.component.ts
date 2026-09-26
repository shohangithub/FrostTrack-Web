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
import { IHeadSummary } from '../income-expense-report/income-expense-report.component';

@Component({
  selector: 'app-income-expense-detail-report',
  templateUrl: './income-expense-detail-report.component.html',
  styleUrls: ['./income-expense-detail-report.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class IncomeExpenseDetailReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  isLoading = false;
  showReport = false;
  today = new Date();

  // Summary (same as the summary report)
  incomeHeads: IHeadSummary[] = [];
  expenseHeads: IHeadSummary[] = [];
  totalIncome = 0;
  totalExpense = 0;
  netBalance = 0;

  // Detailed flat lists
  incomeTransactions: ITransactionListResponse[] = [];
  expenseTransactions: ITransactionListResponse[] = [];
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
          this.buildReport(this.allTransactions);
          this.showReport = true;
          this.isLoading = false;
        },
        error: () => {
          this.toastr.error('Failed to load report data', 'Error');
          this.isLoading = false;
        },
      });
  }

  buildReport(transactions: ITransactionListResponse[]): void {
    const incomeMap = new Map<string, IHeadSummary>();
    const expenseMap = new Map<string, IHeadSummary>();
    const incomeList: ITransactionListResponse[] = [];
    const expenseList: ITransactionListResponse[] = [];

    for (const t of transactions) {
      if (!t.netAmount || t.netAmount <= 0) continue;

      const headName = t.transactionHead?.name || 'Others';
      const displayType = (t.transactionHead?.displayType || '').toUpperCase();
      const headType = (t.transactionHead?.type || '').toUpperCase();
      const desc = (t.description || '').toUpperCase();
      const paymentMethod = (t.paymentMethod || '').toUpperCase();

      // Discount / adjustment entries — include in list but mark separately
      const isDiscount =
        paymentMethod === 'DISCOUNT' ||
        headName.toUpperCase().includes('DISCOUNT') ||
        headName.toUpperCase().includes('ADJUSTMENT') ||
        displayType.includes('DISCOUNT') ||
        displayType.includes('ADJUSTMENT');

      const isExpense = isDiscount
        ? false
        : this.classifyAsExpense(headName, displayType, headType, desc, t);

      // Summary maps (exclude discounts from totals)
      if (!isDiscount) {
        const targetMap = isExpense ? expenseMap : incomeMap;
        const existing = targetMap.get(headName);
        if (existing) {
          existing.count++;
          existing.total += t.netAmount;
        } else {
          targetMap.set(headName, { headName, count: 1, total: t.netAmount });
        }
      }

      // Flat lists (include discounts, they go to expense side as adjustments)
      if (isDiscount || isExpense) {
        expenseList.push(t);
      } else {
        incomeList.push(t);
      }
    }

    this.incomeHeads = [...incomeMap.values()].sort((a, b) => b.total - a.total);
    this.expenseHeads = [...expenseMap.values()].sort((a, b) => b.total - a.total);

    this.totalIncome = this.incomeHeads.reduce((s, h) => s + h.total, 0);
    this.totalExpense = this.expenseHeads.reduce((s, h) => s + h.total, 0);
    this.netBalance = this.totalIncome - this.totalExpense;

    // Sort each list by date
    this.incomeTransactions = incomeList.sort(
      (a, b) =>
        new Date(a.transactionDate).getTime() -
        new Date(b.transactionDate).getTime()
    );
    this.expenseTransactions = expenseList.sort(
      (a, b) =>
        new Date(a.transactionDate).getTime() -
        new Date(b.transactionDate).getTime()
    );
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

  getReceiptNumber(t: ITransactionListResponse): string {
    if (t.bookingNumber) {
      return t.bookingNumber;
    }
    const combined = `${t.description || ''} ${t.note || ''}`;
    const bkMatch = combined.match(/\b(BK-[A-Za-z0-9\-]+)\b/i);
    if (bkMatch) {
      return bkMatch[1];
    }
    return t.transactionCode;
  }

  get maxSummaryRows(): number {
    return Math.max(this.incomeHeads.length, this.expenseHeads.length);
  }

  get summaryRowIndices(): number[] {
    return Array.from({ length: this.maxSummaryRows }, (_, i) => i);
  }

  reset(): void {
    this.showReport = false;
    this.incomeHeads = [];
    this.expenseHeads = [];
    this.totalIncome = 0;
    this.totalExpense = 0;
    this.netBalance = 0;
    this.incomeTransactions = [];
    this.expenseTransactions = [];
    this.allTransactions = [];
  }
}
