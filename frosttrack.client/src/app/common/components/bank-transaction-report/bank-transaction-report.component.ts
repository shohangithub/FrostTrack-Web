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
import { BankTransactionService } from '../../services/bank-transaction.service';
import { IBankTransactionListResponse } from '../../models/bank-transaction.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';

@Component({
  selector: 'app-bank-transaction-report',
  templateUrl: './bank-transaction-report.component.html',
  styleUrls: ['./bank-transaction-report.component.scss'],
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
export class BankTransactionReportComponent {
  reportForm: UntypedFormGroup;
  transactions: IBankTransactionListResponse[] = [];
  isLoading = false;
  showReport = false;
  today = new Date();

  totalDeposit = 0;
  totalWithdrawal = 0;
  netAmount = 0;

  transactionTypeOptions = [
    { value: '', text: 'All Types' },
    { value: 'Deposit', text: 'Deposit' },
    { value: 'Withdraw', text: 'Withdraw' },
    { value: 'Transfer', text: 'Transfer' },
    { value: 'Fee', text: 'Bank Fee' },
    { value: 'Interest', text: 'Interest' },
    { value: 'Other', text: 'Other' },
  ];

  statusOptions = [
    { value: '', text: 'All Status' },
    { value: 'Pending', text: 'Pending' },
    { value: 'Completed', text: 'Completed' },
    { value: 'Cancelled', text: 'Cancelled' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private bankTransactionService: BankTransactionService,
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
      transactionType: [''],
      status: [''],
    });
  }

  onSubmit(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please fill in all required fields');
      return;
    }

    this.loadBankTransactionReport();
  }

  loadBankTransactionReport(): void {
    this.isLoading = true;
    const formValue = this.reportForm.value;

    const startDate = new Date(formValue.startDate);
    const endDate = new Date(formValue.endDate);

    // Get all transactions with pagination (set large page size to get all)
    const paginationQuery = {
      pageIndex: 0,
      pageSize: 10000,
      orderBy: 'transactionDate',
      isAscending: false,
    };

    this.bankTransactionService.getWithPagination(paginationQuery).subscribe({
      next: (response) => {
        // Filter by date range and other criteria
        const filtered = response.data.filter((t) => {
          const transactionDate = new Date(t.transactionDate);
          return transactionDate >= startDate && transactionDate <= endDate;
        });

        this.transactions = this.filterTransactions(filtered, formValue);
        this.calculateTotals();
        this.showReport = true;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.showReport = false;
        this.toastr.error('Failed to load bank transaction report');
      },
    });
  }

  filterTransactions(
    transactions: IBankTransactionListResponse[],
    filters: any
  ): IBankTransactionListResponse[] {
    let filtered = [...transactions];

    if (filters.transactionType) {
      filtered = filtered.filter(
        (t) => t.transactionType === filters.transactionType
      );
    }

    if (filters.status) {
      filtered = filtered.filter((t) => t.status === filters.status);
    }

    return filtered;
  }

  calculateTotals(): void {
    this.totalDeposit = this.transactions
      .filter(
        (t) =>
          t.transactionType === 'Deposit' || t.transactionType === 'Interest'
      )
      .reduce((sum, t) => sum + t.amount, 0);

    this.totalWithdrawal = this.transactions
      .filter(
        (t) => t.transactionType === 'Withdraw' || t.transactionType === 'Fee'
      )
      .reduce((sum, t) => sum + Math.abs(t.amount), 0);

    this.netAmount = this.totalDeposit - this.totalWithdrawal;
  }

  print(): void {
    window.print();
  }

  resetReport(): void {
    this.showReport = false;
    this.transactions = [];
    this.totalDeposit = 0;
    this.totalWithdrawal = 0;
    this.netAmount = 0;
  }

  getTransactionTypeLabel(type: string): string {
    const types: { [key: string]: string } = {
      Deposit: 'Deposit',
      Withdraw: 'Withdraw',
      Transfer: 'Transfer',
      Fee: 'Bank Fee',
      Interest: 'Interest',
      Other: 'Other',
    };
    return types[type] || type;
  }

  getStatusLabel(status: string): string {
    const statuses: { [key: string]: string } = {
      Pending: 'Pending',
      Completed: 'Completed',
      Cancelled: 'Cancelled',
    };
    return statuses[status] || status;
  }

  getTypeBadgeClass(type: string): string {
    if (type === 'Deposit' || type === 'Interest') {
      return 'type-deposit';
    } else if (type === 'Withdraw' || type === 'Fee') {
      return 'type-withdrawal';
    }
    return 'type-other';
  }

  getStatusBadgeClass(status: string): string {
    if (status === 'Completed') {
      return 'status-completed';
    } else if (status === 'Pending') {
      return 'status-pending';
    } else if (status === 'Cancelled') {
      return 'status-cancelled';
    }
    return '';
  }
}
