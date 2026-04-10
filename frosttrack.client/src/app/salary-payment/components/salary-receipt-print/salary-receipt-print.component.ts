import { Component, OnInit, ViewChild, Input, ElementRef } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgxPrintModule, NgxPrintDirective } from 'ngx-print';
import { LayoutService } from '@core/service/layout.service';
import { SalaryPaymentService } from '../../services/salary-payment.service';
import { ISalaryPaymentResponse } from '../../models/salary-payment.interface';
import { ToastrService } from 'ngx-toastr';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ILookup } from '@core/models/lookup';

@Component({
  selector: 'app-salary-receipt-print',
  templateUrl: './salary-receipt-print.component.html',
  styleUrls: ['./salary-receipt-print.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    NgxPrintModule,
    RouterLink,
    ReactiveFormsModule,
    NgSelectModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class SalaryReceiptPrintComponent implements OnInit {
  @ViewChild(NgxPrintDirective) ngxPrintDirective!: NgxPrintDirective;

  @Input() preloadedTransactionId: string = '';
  @Input() hideSearchForm: boolean = false;

  salaryReceipt: ISalaryPaymentResponse | null = null;
  isLoading = true;
  isLookupLoading = false;
  transactionId: string = '';
  receiptTitle = 'বেতন প্রদান স্লিপ';
  backUrl = 'list';
  isPrintFromRoute = false;
  salaryPaymentList: ILookup<string>[] = [];
  criteriaForm: UntypedFormGroup;

  Math = Math;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: UntypedFormBuilder,
    private salaryPaymentService: SalaryPaymentService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
    this.criteriaForm = this.fb.group({
      transactionId: [null, [Validators.required]],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const backurl = this.route.snapshot.paramMap.get('backurl');

    if (this.preloadedTransactionId) {
      this.transactionId = this.preloadedTransactionId;
    } else if (id) {
      this.transactionId = id;
      this.isPrintFromRoute = true;
    }

    if (backurl) {
      this.backUrl = backurl;
    }

    if (this.transactionId) {
      this.loadData();
    } else {
      this.isLoading = false;
      if (!this.hideSearchForm) {
        this.fetchLookup();
      }
    }
  }

  fetchLookup(): void {
    this.isLookupLoading = true;
    this.salaryPaymentService.getLookup().subscribe({
      next: (list: ILookup<string>[]) => {
        this.salaryPaymentList = list;
        this.isLookupLoading = false;
      },
      error: () => {
        this.isLookupLoading = false;
      },
    });
  }

  showReceipt(): void {
    const selected = this.criteriaForm.get('transactionId')?.value;
    if (selected) {
      this.transactionId = selected;
      this.loadData();
    }
  }

  loadData(): void {
    this.isLoading = true;
    this.salaryPaymentService.getById(this.transactionId).subscribe({
      next: (response: ISalaryPaymentResponse) => {
        this.salaryReceipt = response;
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('বেতন তথ্য লোড করা সম্ভব হয়নি', 'ত্রুটি');
        this.isLoading = false;
      },
    });
  }

  triggerPrint(): void {
    if (this.ngxPrintDirective && this.salaryReceipt) {
      this.ngxPrintDirective.print();
    }
  }

  goBack(): void {
    if (this.hideSearchForm && !this.isPrintFromRoute) {
      window.history.back();
      return;
    }
    this.router.navigate([`/salary-payment/${this.backUrl}`]);
  }

  getPaymentMethodLabel(method: string): string {
    const methods: { [key: string]: string } = {
      CASH: 'নগদ',
      BANK_TRANSFER: 'ব্যাংক ট্রান্সফার',
      CHEQUE: 'চেক',
      MOBILE_BANKING: 'মোবাইল ব্যাংকিং',
      CARD: 'কার্ড',
      CREDIT: 'ক্রেডিট',
    };
    return methods[method] || method;
  }

  getMonthName(month: number): string {
    const months = [
      'জানুয়ারি',
      'ফেব্রুয়ারি',
      'মার্চ',
      'এপ্রিল',
      'মে',
      'জুন',
      'জুলাই',
      'আগস্ট',
      'সেপ্টেম্বর',
      'অক্টোবর',
      'নভেম্বর',
      'ডিসেম্বর',
    ];
    return months[month - 1] ?? '';
  }

  convertToWords(amount: number): string {
    if (amount === 0) return 'শূন্য টাকা মাত্র';
    const num = Math.floor(Math.abs(amount));
    const decimal = Math.round((Math.abs(amount) - num) * 100);
    let words = this.convertIntegerToWords(num);
    if (decimal > 0) {
      words += ' এবং ' + this.convertIntegerToWords(decimal) + ' পয়সা';
    }
    return words + ' টাকা মাত্র';
  }

  convertIntegerToWords(num: number): string {
    if (num === 0) return '';
    const ones = [
      '',
      'এক',
      'দুই',
      'তিন',
      'চার',
      'পাঁচ',
      'ছয়',
      'সাত',
      'আট',
      'নয়',
    ];
    const teens = [
      'দশ',
      'এগারো',
      'বারো',
      'তেরো',
      'চৌদ্দ',
      'পনেরো',
      'ষোলো',
      'সতেরো',
      'আঠারো',
      'উনিশ',
    ];
    const tens = [
      '',
      '',
      'বিশ',
      'ত্রিশ',
      'চল্লিশ',
      'পঞ্চাশ',
      'ষাট',
      'সত্তর',
      'আশি',
      'নব্বই',
    ];

    if (num < 10) return ones[num];
    if (num < 20) return teens[num - 10];
    if (num < 100)
      return (
        tens[Math.floor(num / 10)] +
        (num % 10 !== 0 ? ' ' + ones[num % 10] : '')
      );
    if (num < 1000)
      return (
        ones[Math.floor(num / 100)] +
        ' শত' +
        (num % 100 !== 0 ? ' ' + this.convertIntegerToWords(num % 100) : '')
      );
    if (num < 100000)
      return (
        this.convertIntegerToWords(Math.floor(num / 1000)) +
        ' হাজার' +
        (num % 1000 !== 0 ? ' ' + this.convertIntegerToWords(num % 1000) : '')
      );
    if (num < 10000000)
      return (
        this.convertIntegerToWords(Math.floor(num / 100000)) +
        ' লক্ষ' +
        (num % 100000 !== 0
          ? ' ' + this.convertIntegerToWords(num % 100000)
          : '')
      );
    return (
      this.convertIntegerToWords(Math.floor(num / 10000000)) +
      ' কোটি' +
      (num % 10000000 !== 0
        ? ' ' + this.convertIntegerToWords(num % 10000000)
        : '')
    );
  }
}
