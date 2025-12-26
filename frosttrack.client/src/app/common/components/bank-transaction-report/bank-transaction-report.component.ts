import { Component } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { BankTransactionService } from '../../services/bank-transaction.service';
import {
  IBankTransactionListResponse,
  IBankTransactionPaginationQuery,
} from '../../models/bank-transaction.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { BANK_TRANSACTION_TYPE } from 'app/common/data/settings-data';

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
    { value: '', text: 'All' },
    { value: 'DEPOSIT', text: 'Deposit' },
    { value: 'WITHDRAW', text: 'Withdraw' },
    // { value: 'Transfer', text: 'Transfer' },
    // { value: 'Fee', text: 'Bank Fee' },
    // { value: 'Interest', text: 'Interest' },
    // { value: 'Other', text: 'Other' },
  ];

  bankTransactionType = BANK_TRANSACTION_TYPE;
  statusOptions = [
    { value: '', text: 'All' },
    { value: 'active', text: 'Active' },
    { value: 'inactive', text: 'Inactive' },
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
        formatDate(firstDayOfMonth, 'yyyy-MM-dd', 'en-US'),
        Validators.required,
      ],
      endDate: [formatDate(today, 'yyyy-MM-dd', 'en-US'), Validators.required],
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

    // Get all transactions with pagination (set large page size to get all)
    const paginationQuery: IBankTransactionPaginationQuery = {
      pageIndex: 0,
      pageSize: 10000,
      orderBy: 'transactionDate',
      isAscending: true,
      openText: '',
      dateFrom: formValue.startDate,
      dateTo: formValue.endDate,
      transactionType: formValue.transactionType,
      status: formValue.status,
    };

    this.bankTransactionService.getWithPagination(paginationQuery).subscribe({
      next: (response) => {
        this.transactions = response.data;
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

  calculateTotals(): void {
    this.totalDeposit = this.transactions
      .filter((t) => t.transactionType === BANK_TRANSACTION_TYPE.Deposit)
      .reduce((sum, t) => sum + t.amount, 0);

    this.totalWithdrawal = this.transactions
      .filter((t) => t.transactionType === BANK_TRANSACTION_TYPE.Withdraw)
      .reduce((sum, t) => sum + Math.abs(t.amount) * -1, 0);

    this.netAmount = this.totalDeposit + this.totalWithdrawal;
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
      DEPOSIT: 'Deposit',
      WITHDRAW: 'Withdraw',
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
    if (type === BANK_TRANSACTION_TYPE.Deposit || type === 'Interest') {
      return 'type-deposit';
    } else if (type === BANK_TRANSACTION_TYPE.Withdraw || type === 'Fee') {
      return 'type-withdrawal';
    }
    return 'type-other';
  }

  getStatusBadgeClass(status: string): string {
    if (status === BANK_TRANSACTION_TYPE.Deposit) {
      return 'status-completed';
    } else if (status === 'Pending') {
      return 'status-pending';
    } else if (status === BANK_TRANSACTION_TYPE.Withdraw) {
      return 'status-cancelled';
    }
    return '';
  }
}
