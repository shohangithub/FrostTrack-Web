import { DatePipe, CommonModule, DecimalPipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { BookingService } from '../../services/booking.service';
import { IBookingInvoiceWithDeliveryResponse } from '../../models/booking.interface';
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
  selector: 'app-booking-invoice-with-delivery-print',
  templateUrl: './booking-invoice-with-delivery-print.component.html',
  styleUrls: ['./booking-invoice-with-delivery-print.component.scss'],
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
export class BookingInvoiceWithDeliveryPrintComponent implements OnInit {
  @ViewChild('invoiceContent', { static: false }) invoiceContent!: ElementRef;

  bookingInvoice: IBookingInvoiceWithDeliveryResponse | null = null;
  loadingIndicator = true;
  isBookingLoading = false;
  bookingId: string = '';
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
    this.bookingService.getInvoiceWithDelivery(this.bookingId).subscribe({
      next: (response: IBookingInvoiceWithDeliveryResponse) => {
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

  goBack(): void {
    this.router.navigate(['/booking/list']);
  }

  getTotalQuantity(): number {
    if (!this.bookingInvoice?.bookingDetails) return 0;
    return this.bookingInvoice.bookingDetails.reduce(
      (total: number, detail: any) => total + detail.bookingQuantity,
      0
    );
  }

  getTotalAmount(): number {
    if (!this.bookingInvoice?.bookingDetails) return 0;
    return this.bookingInvoice.bookingDetails.reduce(
      (total: number, detail: any) =>
        total + detail.bookingQuantity * detail.bookingRate,
      0
    );
  }

  getLastDeliveryDate(): Date | null {
    if (!this.bookingInvoice?.bookingDetails?.length) {
      return null;
    }
    const lastDeliveryDate =
      this.bookingInvoice.bookingDetails[0].lastDeliveryDate;
    return lastDeliveryDate ? new Date(lastDeliveryDate) : null;
  }

  getRemainingQuantity(productId: number): number {
    if (!this.bookingInvoice) return 0;

    // Find total booked quantity for this product
    const bookedDetail = this.bookingInvoice.bookingDetails.find(
      (bd) => bd.productId === productId
    );
    if (!bookedDetail) return 0;

    // Calculate total delivered quantity for this product
    const totalDelivered = this.bookingInvoice.deliveries.reduce(
      (sum, delivery) => {
        const deliveredDetail = delivery.deliveryDetails.find(
          (dd) => dd.productId === productId
        );
        return sum + (deliveredDetail?.deliveryQuantity || 0);
      },
      0
    );

    return bookedDetail.bookingQuantity - totalDelivered;
  }
}
