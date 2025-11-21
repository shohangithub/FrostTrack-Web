import { DatePipe, CommonModule, DecimalPipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { DeliveryService } from '../../../delivery/services/delivery.service';
import { IDeliveryInvoiceResponse } from '../../../delivery/models/delivery.interface';
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

@Component({
  selector: 'app-delivery-invoice-print',
  templateUrl: './delivery-invoice-print.component.html',
  styleUrls: ['./delivery-invoice-print.component.scss'],
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
export class DeliveryInvoicePrintComponent implements OnInit {
  @ViewChild('invoiceContent', { static: false }) invoiceContent!: ElementRef;

  deliveryInvoice: IDeliveryInvoiceResponse | null = null;
  loadingIndicator = true;
  isDeliveryLoading = false;
  deliveryId: string = '';
  criteriaForm: UntypedFormGroup = this.fb.group({
    deliveryId: [null, [Validators.required]],
  });
  deliveryList: ILookup<string>[] = [];
  private deliveryListSubject: Subject<string> = new Subject<string>();

  constructor(
    private route: ActivatedRoute,
    private fb: UntypedFormBuilder,
    private router: Router,
    private deliveryService: DeliveryService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.fetchDeliveryLookup();
    this.deliveryListSubject.subscribe((value: string) => {
      this.criteriaForm
        .get('deliveryId')
        ?.setValue(this.deliveryList.find((x) => x.value == value));
    });

    // Check if delivery ID is passed via route params
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.deliveryId = id;
      this.loadDeliveryData();
    }
  }

  fetchDeliveryLookup() {
    this.isDeliveryLoading = true;
    this.deliveryService.getDeliveryLookup().subscribe({
      next: (response: ILookup<string>[]) => {
        this.deliveryList = response;
        this.isDeliveryLoading = false;
      },
      error: () => {
        this.isDeliveryLoading = false;
      },
    });
  }

  getDeliveryData() {
    const selectedDelivery = this.criteriaForm.get('deliveryId')?.value;
    if (selectedDelivery) {
      this.deliveryId = selectedDelivery;
      this.loadDeliveryData();
    }
  }

  loadDeliveryData(): void {
    this.loadingIndicator = true;
    this.deliveryService.getInvoiceById(this.deliveryId).subscribe({
      next: (response: IDeliveryInvoiceResponse) => {
        this.deliveryInvoice = response;
        this.loadingIndicator = false;
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
      0
    );
  }

  getTotalBaseQuantity(): number {
    if (!this.deliveryInvoice?.deliveryDetails) return 0;
    return this.deliveryInvoice.deliveryDetails.reduce(
      (total, detail) => total + detail.baseQuantity,
      0
    );
  }

  getTotalAmount(): number {
    if (!this.deliveryInvoice?.deliveryDetails) return 0;
    return this.deliveryInvoice.deliveryDetails.reduce(
      (total, detail) => total + detail.chargeAmount,
      0
    );
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
