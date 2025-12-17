import { DatePipe, CommonModule, DecimalPipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { TransactionService } from '../../services/transaction.service';
import { ITransactionDetailResponse } from '../../models/transaction.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { ILookup } from '@core/models/lookup';
import { Subject } from 'rxjs';
import { NgSelectModule } from '@ng-select/ng-select';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { TRANSACTION_TYPE } from 'app/common/data/settings-data';

/**
 * Reusable Transaction Receipt Print Component
 *
 * Usage Examples:
 *
 * 1. Default standalone page (shows search form):
 *    <app-transaction-receipt-print></app-transaction-receipt-print>
 *
 * 2. For Bill Collection (with custom title and direct transaction ID):
 *    <app-transaction-receipt-print
 *      [receiptTitle]="'বিল সংগ্রহ স্লিপ'"
 *      [preloadedTransactionId]="transactionId"
 *      [hideSearchForm]="true"
 *      [usageForFilter]="'BILL_COLLECTION'">
 *    </app-transaction-receipt-print>
 *
 * 3. For Salary Payment:
 *    <app-transaction-receipt-print
 *      [receiptTitle]="'বেতন প্রদান স্লিপ'"
 *      [preloadedTransactionId]="transactionId"
 *      [hideSearchForm]="true"
 *      [usageForFilter]="'SALARY'">
 *    </app-transaction-receipt-print>
 *
 * @Input receiptTitle - Title displayed on the receipt (default: 'ট্রানজেকশন স্লিপ')
 * @Input preloadedTransactionId - Transaction ID to load directly (skips search)
 * @Input hideSearchForm - Hide the transaction search form (default: false)
 * @Input usageForFilter - Filter transactions by UsageFor type (default: 'TRANSACTION')
 */
@Component({
  selector: 'app-transaction-receipt-print',
  templateUrl: './transaction-receipt-print.component.html',
  styleUrls: ['./transaction-receipt-print.component.scss'],
  standalone: true,
  imports: [
    NgxPrintModule,
    DatePipe,
    DecimalPipe,
    CommonModule,
    NgSelectModule,
    ReactiveFormsModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class TransactionReceiptPrintComponent implements OnInit {
  @ViewChild('receiptContent', { static: false }) receiptContent!: ElementRef;

  // Input properties for reusability
  @Input() receiptTitle: string = 'ট্রানজেকশন স্লিপ'; // Default title
  @Input() preloadedTransactionId: string = ''; // For direct transaction loading
  @Input() hideSearchForm: boolean = false; // Hide search form when used externally
  @Input() usageForFilter: string = 'TRANSACTION'; // Default to TRANSACTION type
  @Input() backRouteModule: string = 'transaction'; // Module path for back navigation (e.g., 'transaction', 'bill-collection')

  transactionReceipt: ITransactionDetailResponse | null = null;
  loadingIndicator = true;
  isTransactionLoading = false;
  transactionId: string = '';
  backUrl: string | null = null;
  _TRANSACTION_TYPE = TRANSACTION_TYPE;
  isPrintFromRoute = false;
  criteriaForm: UntypedFormGroup = this.fb.group({
    transactionId: [null, [Validators.required]],
  });
  transactionList: ILookup<string>[] = [];
  private transactionListSubject: Subject<string> = new Subject<string>();

  constructor(
    private route: ActivatedRoute,
    private fb: UntypedFormBuilder,
    private router: Router,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    // Check if transaction ID is provided as Input
    if (this.preloadedTransactionId) {
      this.transactionId = this.preloadedTransactionId;
      this.isPrintFromRoute = true;
      this.hideSearchForm = true;
      this.loadTransactionData();
      return;
    }

    // Only fetch lookup if search form is visible
    if (!this.hideSearchForm) {
      this.fetchTransactionLookup();
    }

    this.transactionListSubject.subscribe((value: string) => {
      this.criteriaForm
        .get('transactionId')
        ?.setValue(this.transactionList.find((x) => x.value == value));
    });

    // Check if transaction ID is passed via route params
    const id = this.route.snapshot.paramMap.get('id');
    const backurl = this.route.snapshot.paramMap.get('backurl') ?? 'list';
    if (id) {
      this.transactionId = id;
      this.backUrl = backurl;
      this.isPrintFromRoute = true;
      this.loadTransactionData();
    }
  }

  fetchTransactionLookup() {
    this.isTransactionLoading = true;
    this.transactionService.getLookupByUsageFor(this.usageForFilter).subscribe({
      next: (response: ILookup<string>[]) => {
        this.transactionList = response;
        this.isTransactionLoading = false;
      },
      error: () => {
        this.isTransactionLoading = false;
      },
    });
  }

  Math = Math;

  getTransactionData() {
    const selectedTransaction = this.criteriaForm.get('transactionId')?.value;
    if (selectedTransaction) {
      this.transactionId = selectedTransaction;
      this.loadTransactionData();
    }
  }

  loadTransactionData(): void {
    this.loadingIndicator = true;
    this.transactionService.getById(this.transactionId).subscribe({
      next: (response: ITransactionDetailResponse) => {
        this.transactionReceipt = response;
        this.loadingIndicator = false;
      },
      error: () => {
        this.loadingIndicator = false;
      },
    });
  }

  goBack(): void {
    const backPath = this.backUrl || 'list';
    this.router.navigate([`/${this.backRouteModule}/${backPath}`]);
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

  convertToWords(amount: number): string {
    if (amount === 0) return 'Zero only';

    let words = '';
    const num = Math.floor(Math.abs(amount));
    const decimal = Math.round((Math.abs(amount) - num) * 100);

    if (num > 0) {
      words = this.convertIntegerToWords(num);
    }

    if (decimal > 0) {
      if (words) words += ' and ';
      words += this.convertIntegerToWords(decimal) + ' Paisa';
    }

    return words + ' only';
  }

  public convertIntegerToWords(num: number): string {
    if (num === 0) return '';

    const ones = [
      '',
      'One',
      'Two',
      'Three',
      'Four',
      'Five',
      'Six',
      'Seven',
      'Eight',
      'Nine',
    ];
    const teens = [
      'Ten',
      'Eleven',
      'Twelve',
      'Thirteen',
      'Fourteen',
      'Fifteen',
      'Sixteen',
      'Seventeen',
      'Eighteen',
      'Nineteen',
    ];
    const tens = [
      '',
      '',
      'Twenty',
      'Thirty',
      'Forty',
      'Fifty',
      'Sixty',
      'Seventy',
      'Eighty',
      'Ninety',
    ];

    let result = '';

    if (num >= 10000000) {
      result +=
        this.convertIntegerToWords(Math.floor(num / 10000000)) + ' Crore ';
      num %= 10000000;
    }

    if (num >= 100000) {
      result += this.convertIntegerToWords(Math.floor(num / 100000)) + ' Lakh ';
      num %= 100000;
    }

    if (num >= 1000) {
      result +=
        this.convertIntegerToWords(Math.floor(num / 1000)) + ' Thousand ';
      num %= 1000;
    }

    if (num >= 100) {
      result += ones[Math.floor(num / 100)] + ' Hundred ';
      num %= 100;
    }

    if (num >= 20) {
      result += tens[Math.floor(num / 10)] + ' ';
      num %= 10;
    } else if (num >= 10) {
      result += teens[num - 10] + ' ';
      num = 0;
    }

    if (num > 0) {
      result += ones[num] + ' ';
    }

    return result.trim();
  }
}
