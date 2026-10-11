import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { ActivatedRoute, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import {
  BillCollectionService,
  ICustomerBalanceSummary,
} from '../../services/bill-collection.service';
import {
  AdjustmentDirection,
  IBookingAdjustmentLine,
  IBookingPaymentDetail,
  ICustomerDueBookings,
} from '../../models/booking-payment.interface';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { todayInputFormat } from 'app/utils/date-utils';
import { BookingAccountDetailComponent } from '../booking-account-detail/booking-account-detail.component';

/** Booking dropdown entry for adjustments not tied to a booking (opening balance / old dues). */
export const CUSTOMER_LEVEL_OPTION = '__CUSTOMER__';

interface IBookingOption {
  value: string;
  label: string;
  due?: number;
  isContracted?: boolean;
}

/**
 * Discount & Adjustment, booking-wise (same flow as Customer Payment):
 * customer -> booking -> booking account + adjustment history (left) and summary + entry (right).
 * Adjustment − reduces the due (cannot exceed it); Adjustment + adds a charge (booking required).
 * "Customer level (no booking)" allows Adjustment − against the customer total only.
 */
@Component({
  selector: 'app-customer-discount',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgSelectModule, BookingAccountDetailComponent],
  templateUrl: './customer-discount.component.html',
  styleUrls: ['./customer-discount.component.scss'],
})
export class CustomerDiscountComponent implements OnInit {
  readonly CUSTOMER_LEVEL_OPTION = CUSTOMER_LEVEL_OPTION;

  form!: FormGroup;
  customers: ICustomerListResponse[] = [];
  bookingOptions: IBookingOption[] = [];

  dueBookings: ICustomerDueBookings | null = null;
  detail: IBookingPaymentDetail | null = null;
  customerBalance: ICustomerBalanceSummary | null = null;
  history: IBookingAdjustmentLine[] = [];

  customerLoading = false;
  bookingsLoading = false;
  detailLoading = false;
  historyLoading = false;
  isSubmitting = false;
  branchId!: number;

  /** Booking to select once the customer's bookings are loaded (deep link ?bookingId=). */
  private pendingBookingId: string | null = null;

  quickReasons: string[] = [
    'Special Management Waiver (ম্যানেজমেন্ট বিশেষ মওকুফ)',
    'Wastage / Quality Allowance (ক্ষতিপূরণ / মান সমন্বয়)',
    'Early Full Settlement Concession (এককালীন পরিশোধ ছাড়)',
    'Rounding-Off Adjustment (রাউন্ডিং ব্যালেন্স সমন্বয়)',
    'Contractual Volume Discount (চুক্তিবদ্ধ ভলিউম কমিশন/ডিসকাউন্ট)',
    'Customer Dispute Settlement (মীমাংসা সমন্বয়)',
    'Missed / Extra Charge (বাদ পড়া / অতিরিক্ত চার্জ)',
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private customerService: CustomerService,
    private transactionService: TransactionService,
    private authService: AuthService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.branchId = this.authService.currentBranchId;
    this.initForm();
    this.generateCode();
    this.loadCustomers();

    // Deep link from Due List / Booking History: ?customerId=..&bookingId=..
    const params = this.route.snapshot.queryParamMap;
    const customerId = Number(params.get('customerId'));
    this.pendingBookingId = params.get('bookingId');
    if (customerId > 0) this.form.patchValue({ customerId });
  }

  // ── form ──────────────────────────────────────────────────────────────────
  private initForm(): void {
    this.form = this.fb.group({
      transactionCode: ['', Validators.required],
      transactionDate: [todayInputFormat(), Validators.required],
      customerId: [null, Validators.required],
      bookingKey: [null, Validators.required],
      direction: ['MINUS' as AdjustmentDirection, Validators.required],
      amount: [null, [Validators.required, Validators.min(0.01)]],
      reason: ['', [Validators.required, Validators.minLength(3)]],
      note: [''],
    });

    this.form.get('customerId')?.valueChanges.subscribe((customerId) => this.onCustomerChange(customerId));
    this.form.get('bookingKey')?.valueChanges.subscribe((key) => this.onBookingChange(key));
  }

  private generateCode(): void {
    this.transactionService.generateCode().subscribe({
      next: (res: any) => this.form.patchValue({ transactionCode: res.code }),
      error: () => this.toastr.error('Failed to generate voucher number'),
    });
  }

  private loadCustomers(): void {
    this.customerLoading = true;
    this.customerService.getList().subscribe({
      next: (res) => {
        this.customers = res;
        this.customerLoading = false;
      },
      error: () => (this.customerLoading = false),
    });
  }

  // ── selection ─────────────────────────────────────────────────────────────
  private onCustomerChange(customerId: number | null): void {
    this.dueBookings = null;
    this.bookingOptions = [];
    this.clearSelection();
    this.form.patchValue({ bookingKey: null }, { emitEvent: false });
    if (!customerId) return;

    this.bookingsLoading = true;
    // All active bookings: Adjustment + may be needed on a fully paid booking
    this.billCollectionService.getCustomerDueBookings(customerId, true).subscribe({
      next: (res) => {
        this.dueBookings = res;
        this.bookingOptions = [
          ...res.bookings.map((b) => ({
            value: b.bookingId,
            label: `${b.bookingNumber} — ${this.formatDate(b.bookingDate)}`,
            due: b.due,
            isContracted: b.isContracted,
          })),
          { value: CUSTOMER_LEVEL_OPTION, label: 'Customer level (no booking) / গ্রাহক পর্যায়' },
        ];
        this.bookingsLoading = false;

        if (this.pendingBookingId && this.bookingOptions.some((o) => o.value === this.pendingBookingId)) {
          this.form.patchValue({ bookingKey: this.pendingBookingId });
        }
        this.pendingBookingId = null;
      },
      error: () => (this.bookingsLoading = false),
    });
  }

  private onBookingChange(key: string | null): void {
    this.clearSelection();
    if (!key) return;
    this.loadSelection(key);
  }

  private clearSelection(): void {
    this.detail = null;
    this.customerBalance = null;
    this.history = [];
    this.form.patchValue({ direction: 'MINUS', amount: null, reason: '', note: '' }, { emitEvent: false });
    this.form.markAsUntouched();
  }

  /** Loads the booking account (or customer balance) and the adjustment history for the selection. */
  private loadSelection(key: string): void {
    const customerId = this.form.get('customerId')?.value;
    this.detailLoading = true;

    if (key === CUSTOMER_LEVEL_OPTION) {
      this.billCollectionService.getCustomerBalance(customerId).subscribe({
        next: (res) => {
          this.customerBalance = res;
          this.detailLoading = false;
        },
        error: () => (this.detailLoading = false),
      });
    } else {
      this.billCollectionService.getBookingPaymentDetail(key).subscribe({
        next: (res) => {
          this.detail = res;
          this.detailLoading = false;
        },
        error: () => (this.detailLoading = false),
      });
    }

    this.historyLoading = true;
    this.billCollectionService
      .getAdjustmentHistory(customerId, key === CUSTOMER_LEVEL_OPTION ? null : key)
      .subscribe({
        next: (res) => {
          this.history = res;
          this.historyLoading = false;
        },
        error: () => (this.historyLoading = false),
      });
  }

  // ── computed ──────────────────────────────────────────────────────────────
  get isCustomerLevel(): boolean {
    return this.form.get('bookingKey')?.value === CUSTOMER_LEVEL_OPTION;
  }

  get hasSelection(): boolean {
    return !!this.detail || !!this.customerBalance;
  }

  get direction(): AdjustmentDirection {
    return this.form.get('direction')?.value;
  }

  get amount(): number {
    return Number(this.form.get('amount')?.value) || 0;
  }

  /** Current due of the selected booking, or the customer total for "Customer level". */
  get currentDue(): number {
    if (this.isCustomerLevel) return Math.max(this.customerBalance?.netDue ?? 0, 0);
    return this.detail?.summary.due ?? 0;
  }

  get dueAfter(): number {
    return this.round(this.direction === 'PLUS' ? this.currentDue + this.amount : this.currentDue - this.amount);
  }

  get activeHistory(): IBookingAdjustmentLine[] {
    return this.history.filter((h) => !h.isCancelled);
  }

  get totalPlus(): number {
    return this.round(this.activeHistory.filter((h) => h.direction === 'PLUS').reduce((s, h) => s + h.amount, 0));
  }

  get totalMinus(): number {
    return this.round(this.activeHistory.filter((h) => h.direction === 'MINUS').reduce((s, h) => s + h.amount, 0));
  }

  /** First problem with the entry, or null when it can be saved. */
  get validationError(): string | null {
    if (!this.hasSelection) return 'Select a booking';
    if (this.amount <= 0) return 'Enter the adjustment amount';
    if (this.direction === 'PLUS' && this.isCustomerLevel) return 'Adjustment + needs a booking';
    if (this.direction === 'MINUS' && this.amount > Math.max(this.currentDue, 0) + 0.01)
      return `Adjustment − (৳${this.amount.toFixed(2)}) is more than the due (৳${Math.max(this.currentDue, 0).toFixed(2)})`;
    if (String(this.form.get('reason')?.value || '').trim().length < 3) return 'Enter a reason';
    return null;
  }

  setDirection(direction: AdjustmentDirection): void {
    if (direction === 'PLUS' && this.isCustomerLevel) return;
    this.form.patchValue({ direction });
  }

  selectQuickReason(reason: string): void {
    this.form.patchValue({ reason });
  }

  adjustFullDue(): void {
    if (this.currentDue > 0) this.form.patchValue({ direction: 'MINUS', amount: this.round(this.currentDue) });
  }

  // ── save / cancel ─────────────────────────────────────────────────────────
  save(): void {
    const error = this.validationError;
    if (error) {
      this.form.markAllAsTouched();
      this.toastr.error(error);
      return;
    }

    const v = this.form.getRawValue();
    this.isSubmitting = true;
    this.billCollectionService
      .createAdjustment({
        transactionCode: v.transactionCode,
        transactionDate: v.transactionDate,
        branchId: this.branchId,
        customerId: v.customerId,
        bookingId: this.isCustomerLevel ? null : v.bookingKey,
        direction: v.direction,
        amount: this.amount,
        reason: v.reason.trim(),
        note: v.note ? v.note.trim() : null,
      })
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.generateCode();
          this.refresh();
        },
        error: () => (this.isSubmitting = false),
      });
  }

  cancelAdjustment(line: IBookingAdjustmentLine): void {
    const sign = line.direction === 'PLUS' ? '+' : '−';
    Swal.fire({
      title: `Cancel Adjustment ${sign} ৳${line.amount.toFixed(2)}?`,
      text:
        line.direction === 'PLUS'
          ? 'An opposite entry is posted today, so the charge is removed and the history is kept.'
          : 'The reduction is removed and the due goes up again.',
      showCancelButton: true,
      confirmButtonText: 'Yes, cancel it',
      cancelButtonText: 'No',
    }).then((result) => {
      if (!result.value) return;
      this.billCollectionService.cancelAdjustment(line.source, line.id).subscribe({
        next: () => this.refresh(),
      });
    });
  }

  /** Reloads the selection and the booking dropdown dues after a change. */
  private refresh(): void {
    const key = this.form.get('bookingKey')?.value;
    const customerId = this.form.get('customerId')?.value;
    this.clearSelection();
    if (key) this.loadSelection(key);

    this.billCollectionService.getCustomerDueBookings(customerId, true).subscribe({
      next: (res) => {
        this.dueBookings = res;
        for (const option of this.bookingOptions) {
          const booking = res.bookings.find((b) => b.bookingId === option.value);
          if (booking) option.due = booking.due;
        }
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/bill-collection/list']);
  }

  private round(value: number): number {
    return Math.round(value * 100) / 100;
  }

  private formatDate(value: string): string {
    const d = new Date(value);
    return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: '2-digit' }).replace(/ /g, '-');
  }
}
