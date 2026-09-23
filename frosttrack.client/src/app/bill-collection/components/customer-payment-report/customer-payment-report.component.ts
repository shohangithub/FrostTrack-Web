import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { dateInputFormat, todayInputFormat } from 'app/utils/date-utils';
import {
  BillCollectionService,
  ICustomerPaymentReportItem,
} from '../../services/bill-collection.service';

@Component({
  selector: 'app-customer-payment-report',
  templateUrl: './customer-payment-report.component.html',
  styleUrls: ['./customer-payment-report.component.scss'],
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
export class CustomerPaymentReportComponent implements OnInit {
  reportForm!: FormGroup;
  payments: ICustomerPaymentReportItem[] = [];
  customerList: { id: number; name: string }[] = [];
  isLoading = false;
  showReport = false;
  generatedDate = new Date();

  // Summary Metrics
  totalCollected = 0;
  totalTransactions = 0;

  paymentMethodOptions = [
    { value: '', text: 'All Payment Methods' },
    { value: 'CASH', text: 'Cash (নগদ)' },
    { value: 'BANK_TRANSFER', text: 'Bank Transfer' },
    { value: 'CHEQUE', text: 'Cheque' },
    { value: 'MOBILE_BANKING', text: 'Mobile Banking (Bkash/Nagad)' },
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private customerService: CustomerService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

    this.reportForm = this.fb.group({
      startDate: [dateInputFormat(firstDayOfMonth), Validators.required],
      endDate: [todayInputFormat(), Validators.required],
      customerId: [null],
      paymentMethod: [''],
    });

    this.loadCustomerLookup();
  }

  loadCustomerLookup(): void {
    this.customerService.getList().subscribe({
      next: (res: ICustomerListResponse[]) => {
        this.customerList = (res || []).map((c) => ({
          id: c.id,
          name: c.customerCode ? `${c.customerName} (${c.customerCode})` : c.customerName,
        }));
      },
      error: () => {
        this.customerList = [];
      },
    });
  }

  onSubmit(): void {
    if (this.reportForm.invalid) {
      this.toastr.error('Please enter valid dates');
      return;
    }

    this.loadReport();
  }

  loadReport(): void {
    this.isLoading = true;
    const formVal = this.reportForm.value;
    this.generatedDate = new Date();

    this.billCollectionService
      .getCustomerPaymentReport({
        startDate: formVal.startDate,
        endDate: formVal.endDate,
        customerId: formVal.customerId || undefined,
        paymentMethod: formVal.paymentMethod || undefined,
      })
      .subscribe({
        next: (items: ICustomerPaymentReportItem[]) => {
          this.payments = items || [];
          this.calculateSummaries();
          this.showReport = true;
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
          this.showReport = false;
          this.toastr.error('Failed to load customer payment report');
        },
      });
  }

  calculateSummaries(): void {
    this.totalCollected = this.payments.reduce((sum, p) => sum + (p.amount || 0), 0);
    this.totalTransactions = this.payments.length;
  }

  resetReport(): void {
    this.showReport = false;
    this.payments = [];
    this.totalCollected = 0;
    this.totalTransactions = 0;
  }

  getSelectedCustomerName(): string {
    const customerId = this.reportForm.get('customerId')?.value;
    if (!customerId) return 'All Customers';
    const found = this.customerList.find((c) => c.id === customerId);
    return found ? found.name : 'Selected Customer';
  }

  getPaymentMethodLabel(method: string): string {
    const found = this.paymentMethodOptions.find((p) => p.value === method);
    return found ? found.text : method || 'Cash';
  }
}
