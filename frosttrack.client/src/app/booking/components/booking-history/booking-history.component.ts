import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, UntypedFormBuilder, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ILookup } from '@core/models/lookup';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { BookingService } from '../../services/booking.service';
import { IBookingHistory, IBookingStatementLine } from '../../models/booking-history.interface';

/**
 * Booking History: everything that happened on one booking — booking info, items, deliveries
 * (with challan), a date-ordered account statement with running due, adjustments and the due summary.
 * Figures match the payment screen (same charge ledger + payments).
 */
@Component({
  selector: 'app-booking-history',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgSelectModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
  templateUrl: './booking-history.component.html',
  styleUrls: ['./booking-history.component.scss'],
})
export class BookingHistoryComponent implements OnInit {
  criteriaForm = this.fb.group({ bookingId: [null as string | null, Validators.required] });
  bookingList: ILookup<string>[] = [];
  history: IBookingHistory | null = null;

  isBookingLoading = false;
  isLoading = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private bookingService: BookingService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.isBookingLoading = true;
    this.bookingService.getLookup().subscribe({
      next: (res) => {
        this.bookingList = res;
        this.isBookingLoading = false;
      },
      error: () => (this.isBookingLoading = false),
    });

    // Opened from the booking list: /booking/history/:id
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.criteriaForm.patchValue({ bookingId: id });
      this.load(id);
    }
  }

  showHistory(): void {
    const id = this.criteriaForm.get('bookingId')?.value;
    if (id) this.load(id);
  }

  private load(id: string): void {
    this.isLoading = true;
    this.history = null;
    this.bookingService.getHistory(id).subscribe({
      next: (res) => {
        this.history = res;
        this.today = new Date();
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(err?.error?.message || 'Failed to load booking history');
        this.isLoading = false;
      },
    });
  }

  openAdjustment(): void {
    if (!this.history) return;
    this.router.navigate(['/bill-collection/discount'], {
      queryParams: { customerId: this.history.customerId, bookingId: this.history.bookingId },
    });
  }

  // ── totals ────────────────────────────────────────────────────────────────
  get totalBooked(): number {
    return this.history?.items.reduce((s, i) => s + i.bookedQty, 0) ?? 0;
  }

  get totalDelivered(): number {
    return this.history?.items.reduce((s, i) => s + i.deliveredQty, 0) ?? 0;
  }

  get totalInStore(): number {
    return this.history?.items.reduce((s, i) => s + i.stockInStore, 0) ?? 0;
  }

  get totalCharges(): number {
    return this.history?.statement.reduce((s, l) => s + l.charge, 0) ?? 0;
  }

  get totalPaidIn(): number {
    return this.history?.statement.reduce((s, l) => s + l.paid, 0) ?? 0;
  }

  get deliveredRent(): number {
    return this.history?.deliveries.reduce((s, d) => s + d.lines.reduce((x, l) => x + l.rentCharge, 0), 0) ?? 0;
  }

  get deliveredLabour(): number {
    return this.history?.deliveries.reduce((s, d) => s + d.lines.reduce((x, l) => x + l.labourCharge, 0), 0) ?? 0;
  }

  get totalCollected(): number {
    return this.history?.deliveries.reduce((s, d) => s + d.collectedAmount, 0) ?? 0;
  }

  /** Bangla label of a statement line category. */
  categoryLabel(line: IBookingStatementLine): string {
    switch (line.category) {
      case 'RENT':
        return 'ভাড়া';
      case 'LABOUR':
        return 'লেবার';
      case 'DELIVERY_ADJUSTMENT':
        return 'ডেলিভারি সমন্বয়';
      case 'ADJUSTMENT_PLUS':
        return 'সমন্বয় (+)';
      case 'CORRECTION':
        return 'সংশোধন';
      case 'AT_DELIVERY':
        return 'ডেলিভারিতে পরিশোধ';
      case 'PAYMENT':
        return 'পরিশোধ';
      case 'ADVANCE':
        return 'অগ্রিম';
      case 'ADJUSTMENT_MINUS':
        return 'সমন্বয় (−)';
      default:
        return line.category;
    }
  }

  isMoneyIn(line: IBookingStatementLine): boolean {
    return line.paid > 0;
  }
}
