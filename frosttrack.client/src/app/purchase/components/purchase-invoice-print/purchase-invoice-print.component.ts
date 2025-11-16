import { DatePipe, CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { PurchaseService } from '../../services/purchase.service';
import { IPurchaseResponse } from '../../models/purchase.interface';
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
import { ReportHeaderComponent } from '@shared/components/reports/report-header.component/report-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { PurchaseReportService } from 'app/purchase/services/purchase-report.service';

@Component({
  selector: 'app-purchase-invoice-print',
  styleUrls: ['./purchase-invoice-print.component.scss'],
  templateUrl: './purchase-invoice.component.html',
  //template: ``,
  standalone: true,
  imports: [
    NgxPrintModule,
    DatePipe,
    CommonModule,
    RouterLink,
    CommonModule,
    NgSelectModule,
    ReactiveFormsModule,
    ReportHeaderComponent,
    ReportFooterComponent,
  ],
})
export class PurchaseInvoicePrintComponent implements OnInit {
  @ViewChild('invoiceContent', { static: false }) invoiceContent!: ElementRef;

  purchaseInvoice: IPurchaseResponse | null = null;
  loadingIndicator = true;
  isInvoiceLoading = false;
  invoiceId: number = 0;
  criteriaForm: UntypedFormGroup = this.fb.group({
    invoiceId: [null, [Validators.required]],
  });
  invoiceList: ILookup<number>[] = [];
  private invoiceListSubject: Subject<number> = new Subject<number>();

  companyInfo = {
    name: 'i Power Bangladesh',
    products: 'IPS, Online UPS, Solar, Battery, Solar Accessories',
    address:
      'Udayn Market (1st Floor) Shop No: 4, Abrar Fahad Avenue, Opposite GPO, Dhaka 1000',
    phone: '01779966833 / 01682560548',
    email: 'ipowerbangladesh@gmail.com',
    website: 'ipowerbd.com',
    bin: '005334223-0208',
  };

  constructor(
    private route: ActivatedRoute,
    private fb: UntypedFormBuilder,
    private router: Router,
    private purchaseService: PurchaseService,
    private purchaseReportService: PurchaseReportService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    // console.log('PurchaseInvoicePrintComponent ngOnInit called');
    // this.route.params.subscribe((params) => {
    //   this.invoiceId = +params['id'];
    //   console.log('Invoice ID from route:', this.invoiceId);
    //   if (this.invoiceId) {
    //     this.loadInvoiceData();
    //   } else {
    //     console.error('No invoice ID found in route params');
    //     this.toastr.error('Invalid invoice ID');
    //     this.router.navigate(['/purchase/invoices']);
    //   }
    // });
    this.fetchInvoiceLookup();
    this.invoiceListSubject.subscribe((value: number) => {
      this.criteriaForm
        .get('invoiceId')
        ?.setValue(this.invoiceList.find((x) => x.value == value));
    });
  }

  initCriteriaForm() {
    this.criteriaForm = this.fb.group({
      invoiceId: [null, [Validators.required]],
    });
  }

  fetchInvoiceLookup() {
    this.isInvoiceLoading = true;
    this.purchaseService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.invoiceList = response;
        this.isInvoiceLoading = false;
      },
      error: () => {
        this.isInvoiceLoading = false;
      },
    });
  }

  getInvoiceData() {
    const selectedInvoice = this.criteriaForm.get('invoiceId')?.value;
    if (selectedInvoice) {
      this.invoiceId = selectedInvoice;
      this.loadInvoiceData();
    }
  }

  loadInvoiceData(): void {
    console.log('Loading invoice data for ID:', this.invoiceId);
    this.loadingIndicator = true;
    this.purchaseReportService.getInvoiceById(this.invoiceId).subscribe({
      next: (response: IPurchaseResponse) => {
        console.log('Invoice data loaded successfully:', response);
        this.purchaseInvoice = response;
        this.loadingIndicator = false;
      },
      error: (error) => {
        console.error('Failed to load invoice data:', error);
        this.loadingIndicator = false;
      },
    });
  }

  printInvoice(): void {
    window.print();
  }

  goBack(): void {
    this.router.navigate(['/purchase/invoices']);
  }

  downloadPDF(): void {
    // TODO: Implement PDF download functionality
    this.toastr.info('PDF download functionality will be implemented soon');
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', {
      style: 'currency',
      currency: 'BDT',
      minimumFractionDigits: 2,
    }).format(amount);
  }

  convertToWords(amount: number): string {
    if (amount === 0) return 'Zero only';

    let words = '';
    const num = Math.floor(amount);
    const decimal = Math.round((amount - num) * 100);

    // Convert the integer part
    if (num > 0) {
      words = this.convertIntegerToWords(num);
    }

    // Add decimal part if present
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

    // Handle crores
    if (num >= 10000000) {
      result +=
        this.convertIntegerToWords(Math.floor(num / 10000000)) + ' Crore ';
      num %= 10000000;
    }

    // Handle lakhs
    if (num >= 100000) {
      result += this.convertIntegerToWords(Math.floor(num / 100000)) + ' Lakh ';
      num %= 100000;
    }

    // Handle thousands
    if (num >= 1000) {
      result +=
        this.convertIntegerToWords(Math.floor(num / 1000)) + ' Thousand ';
      num %= 1000;
    }

    // Handle hundreds
    if (num >= 100) {
      result += ones[Math.floor(num / 100)] + ' Hundred ';
      num %= 100;
    }

    // Handle tens and ones
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

  getTotalQuantity(): number {
    if (!this.purchaseInvoice?.purchaseDetails) return 0;
    return this.purchaseInvoice.purchaseDetails.reduce(
      (total, detail) => total + detail.purchaseQuantity,
      0
    );
  }

  getPreviousDue(): number {
    // This should come from supplier balance or previous transactions
    // For now, returning 0 as placeholder
    return 0;
  }

  getCurrentDue(): number {
    if (!this.purchaseInvoice) return 0;
    return this.purchaseInvoice.invoiceAmount - this.purchaseInvoice.paidAmount;
  }

  getTotalDue(): number {
    return this.getPreviousDue() + this.getCurrentDue();
  }
}
