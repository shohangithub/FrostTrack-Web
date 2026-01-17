import { DatePipe, CommonModule, DecimalPipe } from '@angular/common';
import {
  Component,
  ElementRef,
  Input,
  OnInit,
  ViewChild,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule, NgxPrintDirective } from 'ngx-print';
import { DeliveryService } from '../../../delivery/services/delivery.service';
import { IDeliveryInvoiceResponse } from '../../../delivery/models/delivery.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';

@Component({
  selector: 'app-delivery-invoice',
  templateUrl: './delivery-invoice.component.html',
  styleUrls: ['./delivery-invoice.component.scss'],
  standalone: true,
  imports: [
    NgxPrintModule,
    DatePipe,
    DecimalPipe,
    CommonModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class DeliveryInvoiceComponent implements OnInit, OnChanges {
  @ViewChild('invoiceContent', { static: false }) invoiceContent!: ElementRef;
  @ViewChild(NgxPrintDirective) ngxPrintDirective!: NgxPrintDirective;

  @Input() invoiceId: string = '';
  @Input() autoPrint: boolean = false;

  deliveryInvoice: IDeliveryInvoiceResponse | null = null;
  loadingIndicator = true;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private deliveryService: DeliveryService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    // Check if invoice ID is passed via route params
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.invoiceId = id;
      this.loadDeliveryData();
    } else if (this.invoiceId) {
      // Load data if invoiceId is provided via @Input
      this.loadDeliveryData();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Watch for changes to invoiceId input and reload data
    if (
      changes['invoiceId'] &&
      !changes['invoiceId'].firstChange &&
      changes['invoiceId'].currentValue
    ) {
      this.loadDeliveryData();
    }

    // Auto-trigger print if autoPrint is enabled
    if (changes['autoPrint'] && changes['autoPrint'].currentValue) {
      setTimeout(() => {
        this.triggerPrint();
      }, 500);
    }
  }

  loadDeliveryData(): void {
    if (!this.invoiceId) {
      return;
    }

    this.loadingIndicator = true;
    this.deliveryService.getInvoiceById(this.invoiceId).subscribe({
      next: (response: IDeliveryInvoiceResponse) => {
        this.deliveryInvoice = response;
        this.loadingIndicator = false;

        // Auto-print after data loads if autoPrint is enabled
        if (this.autoPrint) {
          setTimeout(() => {
            this.triggerPrint();
          }, 300);
        }
      },
      error: (error) => {
        console.error('Failed to load delivery data:', error);
        this.loadingIndicator = false;
        this.toastr.error('Failed to load delivery data');
      },
    });
  }

  printInvoice(): void {
    window.print();
  }

  // Method to trigger print programmatically
  triggerPrint(): void {
    if (this.ngxPrintDirective && this.deliveryInvoice) {
      this.ngxPrintDirective.print();
    }
  }

  goBack(): void {
    this.router.navigate(['/product-delivery/list']);
  }

  downloadPDF(): void {
    this.toastr.info('PDF download functionality will be implemented soon');
  }

  getTotalQuantity(): number {
    if (!this.deliveryInvoice?.deliveryDetails) return 0;
    return this.deliveryInvoice.deliveryDetails.reduce(
      (total, detail) => total + detail.deliveryQuantity,
      0,
    );
  }

  getTotalBaseQuantity(): number {
    if (!this.deliveryInvoice?.deliveryDetails) return 0;
    return this.deliveryInvoice.deliveryDetails.reduce(
      (total, detail) => total + detail.baseQuantity,
      0,
    );
  }

  getTotalAmount(): number {
    if (!this.deliveryInvoice?.deliveryDetails) return 0;
    return this.deliveryInvoice.deliveryDetails.reduce(
      (total, detail) => total + detail.chargeAmount + detail.labourCharge,
      0,
    );
  }

  getBillTypeCycleLabel(billType: string): string {
    switch (billType?.toUpperCase()) {
      case 'MONTHLY':
        return 'মাস';
      case 'DAILY':
        return 'দিন';
      case 'WEEKLY':
        return 'সপ্তাহ';
      case 'YEARLY':
        return 'বছর';
      case 'HOURLY':
        return 'ঘন্টা';
      default:
        return 'মাস';
    }
  }

  convertToWords(amount: number): string {
    if (amount === 0) return 'Zero only';

    let words = '';
    const num = Math.floor(amount);
    const decimal = Math.round((amount - num) * 100);

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
