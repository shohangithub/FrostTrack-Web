import { DatePipe, CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { BookingService } from '../../services/booking.service';
import { IBookingResponse } from '../../models/booking.interface';
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
  selector: 'app-booking-invoice-print',
  templateUrl: './booking-invoice-print.component.html',
  styleUrls: ['./booking-invoice-print.component.scss'],
  standalone: true,
  imports: [
    NgxPrintModule,
    DatePipe,
    CommonModule,
    NgSelectModule,
    ReactiveFormsModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class BookingInvoicePrintComponent implements OnInit {
  @ViewChild('invoiceContent', { static: false }) invoiceContent!: ElementRef;

  bookingInvoice: IBookingResponse | null = null;
  loadingIndicator = true;
  isBookingLoading = false;
  bookingId: string = '';
  backUrl: string | null = null;
  isPrintFromRoute = false;
  criteriaForm: UntypedFormGroup = this.fb.group({
    bookingId: [null, [Validators.required]],
  });
  bookingList: ILookup<string>[] = [];
  private bookingListSubject: Subject<string> = new Subject<string>();

  constructor(
    private route: ActivatedRoute,
    private fb: UntypedFormBuilder,
    private router: Router,
    private bookingService: BookingService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.fetchBookingLookup();
    this.bookingListSubject.subscribe((value: string) => {
      this.criteriaForm
        .get('bookingId')
        ?.setValue(this.bookingList.find((x) => x.value == value));
    });

    // Check if delivery ID is passed via route params
    const id = this.route.snapshot.paramMap.get('id');
    const backurl = this.route.snapshot.paramMap.get('backurl') ?? 'list';
    if (id) {
      this.bookingId = id;
      this.backUrl = backurl;
      this.isPrintFromRoute = true;
      this.loadBookingData();
    }
  }

  fetchBookingLookup() {
    this.isBookingLoading = true;
    this.bookingService.getLookup().subscribe({
      next: (response: ILookup<string>[]) => {
        this.bookingList = response;
        this.isBookingLoading = false;
      },
      error: () => {
        this.isBookingLoading = false;
      },
    });
  }

  getBookingData() {
    const selectedBooking = this.criteriaForm.get('bookingId')?.value;
    if (selectedBooking) {
      this.bookingId = selectedBooking;
      this.loadBookingData();
    }
  }

  loadBookingData(): void {
    this.loadingIndicator = true;
    this.bookingService.getById(this.bookingId).subscribe({
      next: (response: IBookingResponse) => {
        this.bookingInvoice = response;
        this.loadingIndicator = false;
      },
      error: (error) => {
        console.error('Failed to load booking data:', error);
        this.loadingIndicator = false;
        this.toastr.error('Failed to load booking data');
      },
    });
  }

  printInvoice(): void {
    window.print();
  }

  onAfterPrint(): void {
    console.log('Print dialog closed (user printed or canceled)');
    if (this.isPrintFromRoute) {
      this.router.navigate([`/booking/${this.backUrl}` || '/booking/list']);
    }
  }

  goBack(): void {
    this.router.navigate([`/booking/${this.backUrl}` || '/booking/list']);
  }

  downloadPDF(): void {
    this.toastr.info('PDF download functionality will be implemented soon');
  }

  getTotalQuantity(): number {
    if (!this.bookingInvoice?.bookingDetails) return 0;
    return this.bookingInvoice.bookingDetails.reduce(
      (total, detail) => total + detail.bookingQuantity,
      0
    );
  }

  getTotalAmount(): number {
    if (!this.bookingInvoice?.bookingDetails) return 0;
    return this.bookingInvoice.bookingDetails.reduce(
      (total, detail) => total + detail.bookingQuantity * detail.bookingRate,
      0
    );
  }

  getLastDeliveryDate(): Date | null {
    if (!this.bookingInvoice?.bookingDetails?.length) {
      return null;
    }
    // Return the LastDeliveryDate from the first booking detail
    // All details should have the same LastDeliveryDate based on the booking date
    const lastDeliveryDate =
      this.bookingInvoice.bookingDetails[0].lastDeliveryDate;
    return lastDeliveryDate ? new Date(lastDeliveryDate) : null;
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
