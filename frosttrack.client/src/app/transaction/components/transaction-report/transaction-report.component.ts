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
import { TransactionService } from '../../services/transaction.service';
import { TransactionHeadService } from 'app/common/services/transaction-head.service';
import { ITransactionListResponse } from '../../models/transaction.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { TRANSACTION_TYPE } from 'app/common/data/settings-data';
import { dateInputFormat, todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-transaction-report',
  templateUrl: './transaction-report.component.html',
  styleUrls: ['./transaction-report.component.scss'],
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
export class TransactionReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  transactions: ITransactionListResponse[] = [];
  rawTransactions: ITransactionListResponse[] = [];
  isLoading = false;
  showReport = false;
  today = new Date();

  totalIncome = 0;
  totalExpense = 0;
  totalDebit = 0;
  totalCredit = 0;
  totalInflow = 0;
  totalOutflow = 0;
  netAmount = 0;

  _TRANSACTION_TYPE = TRANSACTION_TYPE;
  transactionTypeOptions: { value: string; text: string }[] = [
    { value: '', text: 'All Types' },
  ];

  transactionFlowOptions = [
    { value: '', text: 'All Flows' },
    { value: 'IN', text: 'আয় / Income (Money In)' },
    { value: 'OUT', text: 'খরচ / Expense (Money Out)' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private transactionService: TransactionService,
    private transactionHeadService: TransactionHeadService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    // Initialize form with default date range (current month)
    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.reportForm = this.fb.group({
      startDate: [dateInputFormat(firstDayOfMonth), Validators.required],
      endDate: [todayInputFormat(), Validators.required],
      transactionType: [''],
      transactionFlow: [''],
    });
  }

  ngOnInit(): void {
    this.loadTransactionHeads();
  }

  loadTransactionHeads(): void {
    this.transactionHeadService.getTransactionLookup().subscribe({
      next: (heads) => {
        if (heads && heads.length > 0) {
          const dynamicOptions = heads
            .filter((h) => {
              const name = (h.name || '').toUpperCase();
              return (
                !name.includes('DISCOUNT') &&
                !name.includes('ADJUSTMENT') &&
                !name.includes('ছাড়') &&
                !name.includes('সমন্বয়') &&
                !name.includes('OPENING') &&
                !name.includes('প্রারম্ভিক') &&
                !name.includes('SALARY') &&
                !name.includes('বেতন') &&
                !name.includes('CLOSING') &&
                !name.includes('সমাপনী')
              );
            })
            .map((h) => ({
              value: h.name,
              text: h.name,
            }));
          this.transactionTypeOptions = [
            { value: '', text: 'All Types' },
            ...dynamicOptions,
          ];
        }
      },
      error: (err) => console.error('Failed to load transaction heads', err),
    });
  }

  onSubmit(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.loadTransactionReport();
  }

  loadTransactionReport(): void {
    this.isLoading = true;
    const formValue = this.reportForm.value;

    this.transactionService.getTransactionReport(formValue.startDate, formValue.endDate).subscribe({
      next: (response: ITransactionListResponse[]) => {
        const eligible = (response || []).filter(
          (t) => !this.isExcludedTransaction(t)
        );
        this.rawTransactions = eligible;
        this.transactions = this.filterTransactions(eligible, formValue);
        this.calculateTotals();
        this.showReport = true;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.showReport = false;
      },
    });
  }

  isExcludedTransaction(t: ITransactionListResponse): boolean {
    const headName = (t.transactionHead?.name || '').toUpperCase();
    const headType = (t.transactionHead?.type || '').toUpperCase();
    const displayType = (t.transactionHead?.displayType || '').toUpperCase();
    const desc = (t.description || '').toUpperCase();
    const paymentMethod = (t.paymentMethod || '').toUpperCase();

    // 1. Bill Discount / Adjustment
    if (
      paymentMethod === 'DISCOUNT' ||
      headName.includes('DISCOUNT') ||
      headName.includes('ADJUSTMENT') ||
      headName.includes('ছাড়') ||
      headName.includes('সমন্বয়') ||
      displayType.includes('DISCOUNT') ||
      displayType.includes('ADJUSTMENT') ||
      headType.includes('DISCOUNT') ||
      headType.includes('ADJUSTMENT') ||
      desc.includes('DISCOUNT') ||
      desc.includes('ADJUSTMENT') ||
      desc.includes('ছাড়') ||
      desc.includes('সমন্বয়')
    ) {
      return true;
    }

    // 2. Opening Balance
    if (
      headName.includes('OPENING') ||
      headName.includes('প্রারম্ভিক') ||
      displayType.includes('OPENING') ||
      headType.includes('OPENING') ||
      desc.includes('OPENING BALANCE') ||
      desc.includes('OPENING_BALANCE') ||
      desc.includes('OPENING') ||
      desc.includes('প্রারম্ভিক')
    ) {
      return true;
    }

    // 3. Salary Payment / SALARY
    if (
      headName.includes('SALARY') ||
      headName.includes('বেতন') ||
      displayType.includes('SALARY') ||
      headType.includes('SALARY') ||
      desc.includes('SALARY') ||
      desc.includes('বেতন') ||
      Boolean(t.employeeId && !t.customerId)
    ) {
      return true;
    }

    // 4. Closing Balance
    if (
      headName.includes('CLOSING') ||
      headName.includes('সমাপনী') ||
      displayType.includes('CLOSING') ||
      headType.includes('CLOSING') ||
      desc.includes('CLOSING BALANCE') ||
      desc.includes('CLOSING_BALANCE') ||
      desc.includes('CLOSING') ||
      desc.includes('সমাপনী')
    ) {
      return true;
    }

    return false;
  }

  filterTransactions(
    transactions: ITransactionListResponse[],
    filters: any
  ): ITransactionListResponse[] {
    let filtered = (transactions || []).filter(
      (t) => !this.isExcludedTransaction(t)
    );

    if (filters.transactionType) {
      filtered = filtered.filter(
        (t) =>
          t.transactionHead?.name === filters.transactionType ||
          t.transactionHeadId === filters.transactionType
      );
    }

    if (filters.transactionFlow) {
      filtered = filtered.filter((t) => {
        if (filters.transactionFlow === 'IN') {
          return this.isIncome(t);
        } else if (filters.transactionFlow === 'OUT') {
          return this.isExpense(t);
        }
        return true;
      });
    }

    return filtered;
  }

  isExpense(t: ITransactionListResponse): boolean {
    if (t.paymentMethod === 'DISCOUNT') return false;

    const desc = (t.description || '').toUpperCase();
    const name = (t.transactionHead?.name || '').toUpperCase();
    const type = (t.transactionHead?.type || '').toUpperCase();
    const displayType = (t.transactionHead?.displayType || '').toUpperCase();

    // 1. Explicit expense markers in description (e.g. "লেবার চার্জ প্রদান - EXPENSE")
    if (
      desc.includes('EXPENSE') ||
      desc.includes('OUTFLOW') ||
      desc.includes('CASH OUT') ||
      desc.includes('লেবার চার্জ প্রদান') ||
      desc.includes('চার্জ প্রদান') ||
      desc.includes('বেতন') ||
      desc.includes('SALARY') ||
      desc.includes('খরচ')
    ) {
      return true;
    }

    // 2. Explicit expense markers in displayType or type
    if (
      displayType === 'EXPENSE' ||
      displayType === 'PAYMENT' ||
      displayType === 'OUT' ||
      displayType.includes('CLOSING')
    ) {
      return true;
    }

    if (
      type === 'EXPENSE' ||
      type === 'PAYMENT' ||
      type === 'OUT' ||
      type.includes('CLOSING')
    ) {
      return true;
    }

    // 3. Explicit head name checks
    if (
      name.includes('EXPENSE') ||
      name.includes('SALARY') ||
      name.includes('লেবার') ||
      name.includes('LABOUR') ||
      name.includes('প্রদান') ||
      name.includes('খরচ') ||
      name.includes('BILL_PAYMENT') ||
      name.includes('VENDOR')
    ) {
      return true;
    }

    // 4. If linked to an employee (salary payment)
    if (t.employeeId && !t.customerId) {
      return true;
    }

    return false;
  }

  isIncome(t: ITransactionListResponse): boolean {
    if (t.paymentMethod === 'DISCOUNT') return false;
    return !this.isExpense(t);
  }

  getIncomeAmount(t: ITransactionListResponse): number {
    if (!t.netAmount || t.netAmount <= 0) return 0;
    return this.isIncome(t) ? t.netAmount : 0;
  }

  getExpenseAmount(t: ITransactionListResponse): number {
    if (!t.netAmount || t.netAmount <= 0) return 0;
    return this.isExpense(t) ? t.netAmount : 0;
  }

  // Aliases for compatibility
  isDebit(t: ITransactionListResponse): boolean {
    return this.isIncome(t);
  }

  isCredit(t: ITransactionListResponse): boolean {
    return this.isExpense(t);
  }

  getDebitAmount(t: ITransactionListResponse): number {
    return this.getIncomeAmount(t);
  }

  getCreditAmount(t: ITransactionListResponse): number {
    return this.getExpenseAmount(t);
  }

  calculateTotals(): void {
    this.totalIncome = this.transactions.reduce(
      (sum, t) => sum + this.getIncomeAmount(t),
      0
    );

    this.totalExpense = this.transactions.reduce(
      (sum, t) => sum + this.getExpenseAmount(t),
      0
    );

    this.totalDebit = this.totalIncome;
    this.totalCredit = this.totalExpense;
    this.totalInflow = this.totalIncome;
    this.totalOutflow = this.totalExpense;
    this.netAmount = this.totalIncome - this.totalExpense;
  }

  print(): void {
    window.print();
  }

  resetReport(): void {
    this.showReport = false;
    this.transactions = [];
    this.rawTransactions = [];
    this.totalIncome = 0;
    this.totalExpense = 0;
    this.totalDebit = 0;
    this.totalCredit = 0;
    this.totalInflow = 0;
    this.totalOutflow = 0;
    this.netAmount = 0;
  }

  getTransactionTypeLabel(type: string): string {
    const types: { [key: string]: string } = {
      CUSTOMER_PAYMENT: 'Customer Payment',
      BILL_COLLECTION: 'Bill Collection',
      OFFICE_EXPENSE: 'Office Expense',
      BILL_PAYMENT: 'Bill Payment',
      ADVANCE_PAYMENT: 'Advance Payment',
      OTHER: 'Other',
    };
    return types[type] || type;
  }
}
