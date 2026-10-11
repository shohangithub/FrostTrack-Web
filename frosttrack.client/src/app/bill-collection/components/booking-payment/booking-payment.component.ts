import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { ILookup } from '@core/models/lookup';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { BankService } from 'app/common/services/bank.service';
import { todayInputFormat } from 'app/utils/date-utils';
import { BillCollectionService } from '../../services/bill-collection.service';
import { BookingAccountDetailComponent } from '../booking-account-detail/booking-account-detail.component';
import {
  IBookingPaymentDetail,
  ICustomerAdvances,
  ICustomerDueBookings,
} from '../../models/booking-payment.interface';

/** Value of the booking dropdown entry used to receive an advance (no booking). */
export const ADVANCE_OPTION = '__ADVANCE__';

interface IBookingOption {
  value: string;
  label: string;
  due?: number;
  isContracted?: boolean;
}

/**
 * Customer Payment, booking-wise:
 * customer -> booking with due -> report-style booking information (left) and summary + collection (right).
 * Cash/bank payment + advance applied + discount may not exceed the booking due.
 * Picking "Advance (no booking)" receives money as customer advance instead.
 */
@Component({
  selector: 'app-booking-payment',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgSelectModule, BookingAccountDetailComponent],
  templateUrl: './booking-payment.component.html',
  styleUrls: ['./booking-payment.component.scss'],
})
export class BookingPaymentComponent implements OnInit {
  readonly ADVANCE_OPTION = ADVANCE_OPTION;

  form!: FormGroup;
  customers: ICustomerListResponse[] = [];
  banks: ILookup<number>[] = [];
  bookingOptions: IBookingOption[] = [];

  dueBookings: ICustomerDueBookings | null = null;
  detail: IBookingPaymentDetail | null = null;
  advances: ICustomerAdvances | null = null;

  customerLoading = false;
  bookingsLoading = false;
  detailLoading = false;
  isSubmitting = false;
  branchId!: number;

  paymentMethods = [
    { value: 'CASH', label: 'Cash (নগদ)' },
    { value: 'BANK_TRANSFER', label: 'Bank Transfer (ব্যাংক ট্রান্সফার)' },
    { value: 'CHEQUE', label: 'Bank Cheque (চেক)' },
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private customerService: CustomerService,
    private bankService: BankService,
    private transactionService: TransactionService,
    private authService: AuthService,
    private toastr: ToastrService,
    private router: Router,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.branchId = this.authService.currentBranchId;
    this.initForm();
    this.generateCode();
    this.loadCustomers();
    this.bankService.getLookup().subscribe({ next: (res) => (this.banks = res) });
  }

  // ── form ──────────────────────────────────────────────────────────────────
  private initForm(): void {
    this.form = this.fb.group({
      transactionCode: ['', Validators.required],
      transactionDate: [todayInputFormat(), Validators.required],
      customerId: [null, Validators.required],
      bookingKey: [null, Validators.required],
      cashAmount: [0, [Validators.min(0)]],
      paymentMethod: ['CASH', Validators.required],
      bankId: [null],
      paymentReference: [''],
      note: [''],
      advanceAmount: [0, [Validators.min(0)]],
      discountAmount: [0, [Validators.min(0)]],
      discountReason: [''],
    });

    this.form.get('customerId')?.valueChanges.subscribe((customerId) => this.onCustomerChange(customerId));
    this.form.get('bookingKey')?.valueChanges.subscribe((key) => this.onBookingChange(key));
    this.form.get('paymentMethod')?.valueChanges.subscribe((pm) => {
      if (pm === 'CASH') this.form.patchValue({ bankId: null }, { emitEvent: false });
    });
  }

  private generateCode(): void {
    this.transactionService.generateCode().subscribe({
      next: (res: any) => this.form.patchValue({ transactionCode: res.code }),
      error: () => this.toastr.error('Failed to generate receipt number'),
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
    this.detail = null;
    this.advances = null;
    this.bookingOptions = [];
    this.form.patchValue({ bookingKey: null }, { emitEvent: false });
    if (!customerId) return;

    this.bookingsLoading = true;
    this.billCollectionService.getCustomerDueBookings(customerId).subscribe({
      next: (res) => {
        this.dueBookings = res;
        this.bookingOptions = [
          ...res.bookings.map((b) => ({
            value: b.bookingId,
            label: `${b.bookingNumber} — ${this.formatDate(b.bookingDate)}`,
            due: b.due,
            isContracted: b.isContracted,
          })),
          { value: ADVANCE_OPTION, label: 'Advance (no booking) / অগ্রিম' },
        ];
        this.bookingsLoading = false;
      },
      error: () => (this.bookingsLoading = false),
    });
  }

  private onBookingChange(key: string | null): void {
    this.detail = null;
    this.advances = null;
    this.resetAmounts();
    if (!key) return;

    const customerId = this.form.get('customerId')?.value;
    this.detailLoading = true;

    if (key === ADVANCE_OPTION) {
      this.loadAdvances(customerId);
      return;
    }

    this.billCollectionService.getBookingPaymentDetail(key).subscribe({
      next: (res) => {
        this.detail = res;
        this.form.patchValue({ cashAmount: Math.max(this.round(res.summary.due), 0) }, { emitEvent: false });
        this.detailLoading = false;
      },
      error: () => (this.detailLoading = false),
    });
  }

  private loadAdvances(customerId: number): void {
    this.detailLoading = true;
    this.billCollectionService.getCustomerAdvances(customerId).subscribe({
      next: (res) => {
        this.advances = res;
        this.detailLoading = false;
      },
      error: () => (this.detailLoading = false),
    });
  }

  private resetAmounts(): void {
    this.form.patchValue(
      { cashAmount: 0, advanceAmount: 0, discountAmount: 0, discountReason: '', paymentReference: '', note: '' },
      { emitEvent: false },
    );
  }

  // ── computed ──────────────────────────────────────────────────────────────
  get isAdvanceMode(): boolean {
    return this.form.get('bookingKey')?.value === ADVANCE_OPTION;
  }

  get selectedOption(): IBookingOption | undefined {
    return this.bookingOptions.find((o) => o.value === this.form.get('bookingKey')?.value);
  }

  get cashAmount(): number {
    return Number(this.form.get('cashAmount')?.value) || 0;
  }

  get advanceAmount(): number {
    return Number(this.form.get('advanceAmount')?.value) || 0;
  }

  get discountAmount(): number {
    return Number(this.form.get('discountAmount')?.value) || 0;
  }

  /** Bookings in the dropdown with a due / with a credit (paid more than charged). */
  get dueCount(): number {
    return this.dueBookings?.bookings.filter((b) => b.due > 0.005).length ?? 0;
  }

  get creditCount(): number {
    return this.dueBookings?.bookings.filter((b) => b.due < -0.005).length ?? 0;
  }

  openAdjustment(): void {
    if (!this.detail) return;
    this.router.navigate(['/bill-collection/discount'], {
      queryParams: { customerId: this.detail.customerId, bookingId: this.detail.bookingId },
    });
  }

  get bookingDue(): number {
    return Math.max(this.detail?.summary.due ?? 0, 0);
  }

  get availableAdvance(): number {
    return Math.max(this.detail?.availableAdvance ?? 0, 0);
  }

  get totalEntered(): number {
    return this.round(this.cashAmount + this.advanceAmount + this.discountAmount);
  }

  get dueAfter(): number {
    return this.round(this.bookingDue - this.totalEntered);
  }

  get needsBank(): boolean {
    const pm = this.form.get('paymentMethod')?.value;
    return pm === 'BANK_TRANSFER' || pm === 'CHEQUE';
  }

  /** First problem with the entered amounts, or null when the payment can be saved. */
  get validationError(): string | null {
    if (this.cashAmount < 0 || this.advanceAmount < 0 || this.discountAmount < 0) return 'Amounts cannot be negative';

    if (this.isAdvanceMode) {
      if (this.cashAmount <= 0) return 'Enter the advance amount';
    } else {
      if (!this.detail) return 'Select a booking';
      if (this.totalEntered <= 0) return 'Enter a payment, advance or discount amount';
      if (this.totalEntered > this.bookingDue + 0.01)
        return `Total (৳${this.totalEntered.toFixed(2)}) is more than the booking due (৳${this.bookingDue.toFixed(2)})`;
      if (this.advanceAmount > this.availableAdvance + 0.01)
        return `Only ৳${this.availableAdvance.toFixed(2)} advance is available`;
      if (this.discountAmount > 0 && !String(this.form.get('discountReason')?.value || '').trim())
        return 'Enter a reason for the discount / adjustment';
    }

    if (this.cashAmount > 0 && this.needsBank && !this.form.get('bankId')?.value) return 'Select the bank';
    return null;
  }

  payFullDue(): void {
    const rest = Math.max(this.bookingDue - this.advanceAmount - this.discountAmount, 0);
    this.form.patchValue({ cashAmount: this.round(rest) });
  }

  useMaxAdvance(): void {
    const room = Math.max(this.bookingDue - this.discountAmount, 0);
    const advance = Math.min(this.availableAdvance, room);
    const cash = Math.max(this.bookingDue - this.discountAmount - advance, 0);
    this.form.patchValue({ advanceAmount: this.round(advance), cashAmount: this.round(Math.min(this.cashAmount, cash)) });
  }

  // ── save ──────────────────────────────────────────────────────────────────
  save(print = false): void {
    if (this.form.get('transactionCode')?.invalid || this.form.get('customerId')?.invalid || this.form.get('bookingKey')?.invalid) {
      this.form.markAllAsTouched();
      this.toastr.error('Select a customer and a booking');
      return;
    }
    const error = this.validationError;
    if (error) {
      this.toastr.error(error);
      return;
    }

    const v = this.form.getRawValue();
    this.isSubmitting = true;

    if (this.isAdvanceMode) {
      this.billCollectionService
        .createCustomerPayment({
          transactionCode: v.transactionCode,
          transactionDate: v.transactionDate,
          branchId: this.branchId,
          customerId: v.customerId,
          amount: this.cashAmount,
          paymentMethod: v.paymentMethod,
          bankId: this.needsBank ? v.bankId : null,
          paymentReference: v.paymentReference,
          note: v.note,
          bookingId: null,
        })
        .subscribe({
          next: (res) => this.afterSave(print ? res.id : null),
          error: () => (this.isSubmitting = false),
        });
      return;
    }

    this.billCollectionService
      .createBookingPayment({
        transactionCode: v.transactionCode,
        transactionDate: v.transactionDate,
        branchId: this.branchId,
        customerId: v.customerId,
        bookingId: v.bookingKey,
        cashAmount: this.cashAmount,
        paymentMethod: v.paymentMethod,
        paymentReference: v.paymentReference,
        bankId: this.cashAmount > 0 && this.needsBank ? v.bankId : null,
        note: v.note,
        advanceAmount: this.advanceAmount,
        discountAmount: this.discountAmount,
        discountReason: this.discountAmount > 0 ? v.discountReason : null,
      })
      .subscribe({
        next: (res) => this.afterSave(print ? (res.paymentTransactionId ?? null) : null),
        error: () => (this.isSubmitting = false),
      });
  }

  private afterSave(printTransactionId: string | null): void {
    this.isSubmitting = false;
    if (printTransactionId) {
      this.router.navigate(['/transaction/receipt-print', printTransactionId, 'bill-collection-list']);
      return;
    }
    // Stay on the customer: refresh dues so the next booking can be paid
    const customerId = this.form.get('customerId')?.value;
    this.generateCode();
    this.onCustomerChange(customerId);
  }

  removeAllocation(allocationId: string): void {
    Swal.fire({
      title: 'Remove this advance application?',
      text: 'The amount goes back to the customer advance and the booking becomes due again.',
      showCancelButton: true,
      confirmButtonText: 'Yes, remove',
    }).then((result) => {
      if (!result.value) return;
      this.billCollectionService.removeAdvanceAllocation(allocationId).subscribe({
        next: () => this.loadAdvances(this.form.get('customerId')?.value),
      });
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
