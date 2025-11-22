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
import { TransactionService } from '../../services/transaction.service';
import { ITransactionListResponse } from '../../models/transaction.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';

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
export class TransactionReportComponent {
  reportForm: UntypedFormGroup;
  transactions: ITransactionListResponse[] = [];
  isLoading = false;
  showReport = false;
  today = new Date();

  totalInflow = 0;
  totalOutflow = 0;
  netAmount = 0;

  transactionTypeOptions = [
    { value: '', text: 'All Types' },
    { value: 'BILL_COLLECTION', text: 'Bill Collection' },
    { value: 'OFFICE_EXPENSE', text: 'Office Expense' },
    { value: 'BILL_PAYMENT', text: 'Bill Payment' },
    { value: 'ADVANCE_PAYMENT', text: 'Advance Payment' },
    { value: 'OTHER', text: 'Other' },
  ];

  transactionFlowOptions = [
    { value: '', text: 'All Flows' },
    { value: 'IN', text: 'Inflow (Money Received)' },
    { value: 'OUT', text: 'Outflow (Money Paid)' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private transactionService: TransactionService,
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
      transactionFlow: [''],
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

    const startDate = new Date(formValue.startDate);
    const endDate = new Date(formValue.endDate);

    // Call the summary or cash-flow endpoint
    this.transactionService.getTransactionReport(startDate, endDate).subscribe({
      next: (response: ITransactionListResponse[]) => {
        this.transactions = this.filterTransactions(response, formValue);
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

  filterTransactions(
    transactions: ITransactionListResponse[],
    filters: any
  ): ITransactionListResponse[] {
    let filtered = [...transactions];

    if (filters.transactionType) {
      filtered = filtered.filter(
        (t) => t.transactionType === filters.transactionType
      );
    }

    if (filters.transactionFlow) {
      filtered = filtered.filter(
        (t) => t.transactionFlow === filters.transactionFlow
      );
    }

    return filtered;
  }

  calculateTotals(): void {
    this.totalInflow = this.transactions
      .filter((t) => t.transactionFlow === 'IN')
      .reduce((sum, t) => sum + t.netAmount, 0);

    this.totalOutflow = this.transactions
      .filter((t) => t.transactionFlow === 'OUT')
      .reduce((sum, t) => sum + Math.abs(t.netAmount), 0);

    this.netAmount = this.totalInflow - this.totalOutflow;
  }

  print(): void {
    window.print();
  }

  resetReport(): void {
    this.showReport = false;
    this.transactions = [];
    this.totalInflow = 0;
    this.totalOutflow = 0;
    this.netAmount = 0;
  }

  getTransactionTypeLabel(type: string): string {
    const types: { [key: string]: string } = {
      BILL_COLLECTION: 'Bill Collection',
      OFFICE_EXPENSE: 'Office Expense',
      BILL_PAYMENT: 'Bill Payment',
      ADVANCE_PAYMENT: 'Advance Payment',
      OTHER: 'Other',
    };
    return types[type] || type;
  }

  getPaymentMethodLabel(method: string): string {
    const methods: { [key: string]: string } = {
      CASH: 'Cash',
      BANK_TRANSFER: 'Bank Transfer',
      CHEQUE: 'Cheque',
      MOBILE_BANKING: 'Mobile Banking',
      CARD: 'Card',
      OTHER: 'Other',
    };
    return methods[method] || method;
  }

  getFlowBadgeClass(flow: string): string {
    return flow === 'IN' ? 'badge-success' : 'badge-danger';
  }

  getFlowLabel(flow: string): string {
    return flow === 'IN' ? 'IN' : 'OUT';
  }
}
