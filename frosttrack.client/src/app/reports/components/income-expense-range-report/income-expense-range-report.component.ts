import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { ITransactionListResponse } from 'app/transaction/models/transaction.interface';
import { CashBookService } from '../../services/cashbook.service';
import { IHeadSummary } from '../income-expense-report/income-expense-report.component';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface IDailyIncomeExpense {
  date: string; // yyyy-MM-dd (local)
  income: number;
  expense: number;
  net: number;
  balance: number; // running balance at end of day
}

/** Date-range version of the Income & Expense statement. */
@Component({
  selector: 'app-income-expense-range-report',
  templateUrl: './income-expense-range-report.component.html',
  styleUrls: [
    '../income-expense-report/income-expense-report.component.scss',
    './income-expense-range-report.component.scss',
  ],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class IncomeExpenseRangeReportComponent {
  reportForm: UntypedFormGroup;
  isLoading = false;
  showReport = false;
  today = new Date();

  // Values of the range that is currently displayed
  fromDate = '';
  toDate = '';

  openingBalance = 0;
  closingBalance = 0;
  totalIncome = 0;
  totalExpense = 0;
  netBalance = 0;

  incomeHeads: IHeadSummary[] = [];
  expenseHeads: IHeadSummary[] = [];
  dailyRows: IDailyIncomeExpense[] = [];

  constructor(
    private fb: UntypedFormBuilder,
    private transactionService: TransactionService,
    private cashBookService: CashBookService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    this.reportForm = this.fb.group(
      {
        fromDate: [this.firstDayOfMonth(), Validators.required],
        toDate: [todayInputFormat(), Validators.required],
      },
      { validators: IncomeExpenseRangeReportComponent.rangeValidator },
    );
  }

  static rangeValidator(group: AbstractControl): ValidationErrors | null {
    const from = group.get('fromDate')?.value;
    const to = group.get('toDate')?.value;
    return from && to && from > to ? { invalidRange: true } : null;
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      this.toastr.error(
        this.reportForm.hasError('invalidRange')
          ? 'From date cannot be after To date'
          : 'Please select a date range',
        'Error',
      );
      return;
    }

    this.isLoading = true;
    const { fromDate, toDate } = this.reportForm.value;

    forkJoin({
      transactions: this.transactionService.getTransactionReport(fromDate, toDate),
      // Opening balance of the range = cash book opening balance on the first day
      cashBook: this.cashBookService
        .getCashBook(new Date(fromDate))
        .pipe(catchError(() => of({ openingBalance: 0 } as any))),
    }).subscribe({
      next: ({ transactions, cashBook }) => {
        this.fromDate = fromDate;
        this.toDate = toDate;
        this.openingBalance = cashBook?.openingBalance || 0;
        this.buildReport(transactions || []);
        this.showReport = true;
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load report data', 'Error');
        this.isLoading = false;
      },
    });
  }

  private buildReport(transactions: ITransactionListResponse[]): void {
    const incomeMap = new Map<string, IHeadSummary>();
    const expenseMap = new Map<string, IHeadSummary>();
    const dayMap = new Map<string, { income: number; expense: number }>();

    for (const t of transactions) {
      if (!t.netAmount || t.netAmount <= 0) continue;

      const headName = t.transactionHead?.name || 'Others';
      if (this.isNonCashEntry(t, headName)) continue;

      const isExpense = this.classifyAsExpense(t, headName);

      const targetMap = isExpense ? expenseMap : incomeMap;
      const head = targetMap.get(headName);
      if (head) {
        head.count++;
        head.total += t.netAmount;
      } else {
        targetMap.set(headName, { headName, count: 1, total: t.netAmount });
      }

      const dayKey = this.toLocalDateKey(t.transactionDate);
      const day = dayMap.get(dayKey) ?? { income: 0, expense: 0 };
      if (isExpense) day.expense += t.netAmount;
      else day.income += t.netAmount;
      dayMap.set(dayKey, day);
    }

    this.incomeHeads = [...incomeMap.values()].sort((a, b) => b.total - a.total);
    this.expenseHeads = [...expenseMap.values()].sort((a, b) => b.total - a.total);

    this.totalIncome = this.incomeHeads.reduce((s, h) => s + h.total, 0);
    this.totalExpense = this.expenseHeads.reduce((s, h) => s + h.total, 0);
    this.netBalance = this.totalIncome - this.totalExpense;
    this.closingBalance = this.openingBalance + this.netBalance;

    // Day-wise rows with running balance (only days that have entries)
    let running = this.openingBalance;
    this.dailyRows = [...dayMap.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([date, v]) => {
        running += v.income - v.expense;
        return {
          date,
          income: v.income,
          expense: v.expense,
          net: v.income - v.expense,
          balance: running,
        };
      });
  }

  /** Discounts, adjustments and opening/closing entries are not real income or expense. */
  private isNonCashEntry(t: ITransactionListResponse, headName: string): boolean {
    const name = headName.toUpperCase();
    const displayType = (t.transactionHead?.displayType || '').toUpperCase();
    const paymentMethod = (t.paymentMethod || '').toUpperCase();

    const isDiscountOrAdjustment =
      paymentMethod === 'DISCOUNT' ||
      name.includes('DISCOUNT') ||
      name.includes('ADJUSTMENT') ||
      displayType.includes('DISCOUNT') ||
      displayType.includes('ADJUSTMENT');

    const isOpeningOrClosing =
      name.includes('OPENING') ||
      name.includes('CLOSING') ||
      headName.includes('ওপেনিং') ||
      headName.includes('সমাপনী') ||
      displayType.includes('OPENING') ||
      displayType.includes('CLOSING');

    return isDiscountOrAdjustment || isOpeningOrClosing;
  }

  /** Same rules as the daily Income & Expense report: DEBIT = money in, CREDIT = money out. */
  private classifyAsExpense(t: ITransactionListResponse, headName: string): boolean {
    const headType = (t.transactionHead?.type || '').toUpperCase().trim();
    if (headType === 'DEBIT') return false;
    if (headType === 'CREDIT') return true;

    const displayType = (t.transactionHead?.displayType || '').toUpperCase().trim();
    if (displayType === 'INCOME' || displayType === 'আয়') return false;
    if (displayType === 'EXPENSE' || displayType === 'ব্যয়') return true;

    const name = headName.toUpperCase();
    const desc = (t.description || '').toUpperCase();
    if (
      name.includes('EXPENSE') ||
      name.includes('খরচ') ||
      name.includes('প্রদান') ||
      name.includes('বেতন') ||
      name.includes('SALARY') ||
      desc.includes('EXPENSE') ||
      desc.includes('খরচ') ||
      desc.includes('বেতন')
    ) {
      return true;
    }

    return Boolean(t.employeeId && !t.customerId);
  }

  private toLocalDateKey(value: Date | string): string {
    const d = new Date(value);
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${d.getFullYear()}-${month}-${day}`;
  }

  private firstDayOfMonth(): string {
    const now = new Date();
    const month = (now.getMonth() + 1).toString().padStart(2, '0');
    return `${now.getFullYear()}-${month}-01`;
  }

  get rowIndices(): number[] {
    const max = Math.max(this.incomeHeads.length, this.expenseHeads.length);
    return Array.from({ length: max }, (_, i) => i);
  }

  reset(): void {
    this.showReport = false;
    this.openingBalance = 0;
    this.closingBalance = 0;
    this.totalIncome = 0;
    this.totalExpense = 0;
    this.netBalance = 0;
    this.incomeHeads = [];
    this.expenseHeads = [];
    this.dailyRows = [];
  }
}
