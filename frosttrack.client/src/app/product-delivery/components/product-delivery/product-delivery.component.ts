import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  FormsModule,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { ActivatedRoute, Router } from '@angular/router';
import { DeliveryService } from 'app/delivery/services/delivery.service';
import { BookingService } from 'app/booking/services/booking.service';
import { ICustomerOutstandingResponse } from 'app/booking/models/booking.interface';
import {
  IBookingForDeliveryResponse,
  IDeliveryRequest,
} from 'app/delivery/models/delivery.interface';
import { BankService } from 'app/common/services/bank.service';
import { ILookup } from '@core/models/lookup';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';
import { DeliveryInvoiceComponent } from '../delivery-invoice/delivery-invoice.component';

@Component({
  selector: 'app-delivery',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    DeliveryInvoiceComponent,
  ],
  templateUrl: './product-delivery.component.html',
})
export class DeliveryComponent implements OnInit {
  @ViewChild(DeliveryInvoiceComponent)
  invoiceComponent!: DeliveryInvoiceComponent;

  deliveryForm!: FormGroup;
  bookingData: IBookingForDeliveryResponse | null = null;
  bookings: { value: string; text: string }[] = [];
  bookingLoading = false;
  isLoading = false;
  isSubmitting = false;
  deliveryNumber = '';
  isEditMode = false;

  /** Contract (চুক্তি) bookings are billed per cycle on the bill statement, not paid at delivery. */
  get isContractBooking(): boolean {
    return !!this.bookingData?.isContracted;
  }

  // For printing
  invoiceId: string = '';
  showInvoice: boolean = false;
  shouldAutoPrint: boolean = false;
  customerOutstanding: ICustomerOutstandingResponse | null = null;
  outstandingLoading: boolean = false;

  banks: ILookup<number>[] = [];

  paymentMethods = [
    { value: 'CASH', label: 'Cash (নগদ)' },
    { value: 'BANK_TRANSFER', label: 'Bank Transfer (ব্যাংক ট্রান্সফার)' },
    { value: 'CHEQUE', label: 'Cheque (চেক)' },
    { value: 'MOBILE_BANKING', label: 'Mobile Banking (মোবাইল ব্যাংকিং)' },
  ];

  constructor(
    private fb: FormBuilder,
    private deliveryService: DeliveryService,
    private bookingService: BookingService,
    private bankService: BankService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
  ) { }

  ngOnInit() {
    this.initForm();
    this.loadBookingLookup();
    this.loadBanks();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.loadExistingDelivery(id);
    } else {
      this.generateDeliveryNumber();
    }
  }

  initForm() {
    this.deliveryForm = this.fb.group({
      deliveryNumber: ['', Validators.required],
      deliveryDate: [
        new Date().toISOString().split('T')[0],
        Validators.required,
      ],
      bookingId: ['', Validators.required],
      notes: [''],
      chargeAmount: [0, [Validators.required, Validators.min(0)]],
      totalPreviousPayments: [0], // Track previous payments
      remainingBalance: [0], // Balance after deducting previous payments
      deliveryDetails: this.fb.array([]),
      // Transaction fields
      createTransaction: [true], // Changed to true by default
      transactionAmount: [null],
      paymentMethod: ['CASH'],
      bankId: [null],
      paymentReference: [''],
      transactionNotes: [''],
    });

    // Watch for booking changes
    this.deliveryForm.get('bookingId')?.valueChanges.subscribe((bookingId) => {
      if (bookingId) {
        this.onBookingChange(bookingId);
      }
    });

    // Watch for payment method changes to toggle bank requirement
    this.deliveryForm.get('paymentMethod')?.valueChanges.subscribe((pm) => {
      const bankControl = this.deliveryForm.get('bankId');
      if (pm === 'BANK_TRANSFER' || pm === 'CHEQUE') {
        bankControl?.setValidators([Validators.required]);
      } else {
        bankControl?.clearValidators();
        bankControl?.setValue(null);
      }
      bankControl?.updateValueAndValidity();
    });

    // Watch for delivery date changes to recalculate billing cycles
    this.deliveryForm.get('deliveryDate')?.valueChanges.subscribe(() => {
      this.recalculateAllCharges();
    });
  }

  loadBanks() {
    this.bankService.getLookup().subscribe({
      next: (res) => (this.banks = res),
      error: (err) => console.error('Failed to load banks:', err),
    });
  }

  get deliveryDetails(): FormArray {
    return this.deliveryForm.get('deliveryDetails') as FormArray;
  }

  generateDeliveryNumber() {
    this.deliveryService.getDeliveryNumber().subscribe({
      next: (response) => {
        this.deliveryNumber = response.code;
        this.deliveryForm.patchValue({ deliveryNumber: response.code });
      },
      error: () => {
        this.toastr.error('Failed to generate delivery number');
      },
    });
  }

  loadBookingLookup() {
    this.bookingLoading = true;
    this.deliveryService.getBookingLookup().subscribe({
      next: (bookings) => {
        this.bookings = bookings;
        this.bookingLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load bookings');
        this.bookingLoading = false;
      },
    });
  }

  onBookingChange(bookingId: string) {
    if (!bookingId) return;

    const selectedBooking = this.bookings.find((b) => b.value === bookingId);
    if (!selectedBooking) return;

    this.isLoading = true;
    this.deliveryService
      .getBookingForDelivery(selectedBooking.value)
      .subscribe({
        next: (booking) => {
          this.bookingData = booking;
          this.populateDeliveryDetails(booking);
          this.isLoading = false;
        },
        error: (err) => {
          this.isLoading = false;
          this.toastr.error(err.error?.message || 'Booking not found');
        },
      });
  }

  loadCustomerOutstanding(customerId: number) {
    // No longer used: delivery form focuses strictly on this delivery's charges
  }

  populateDeliveryDetails(
    booking: IBookingForDeliveryResponse,
    onReady?: () => void,
  ) {
    if (this.deliveryDetails.length > 0) this.deliveryDetails.clear();

    setTimeout(() => {
      booking.bookingDetails.forEach((detail) => {
        if (detail.remainingQuantity > 0) {
          const detailForm = this.fb.group({
            bookingDetailId: [detail.id, Validators.required],
            productId: [detail.productId],
            productName: [detail.productName],
            bookingUnitId: [detail.bookingUnitId],
            bookingQuantity: [detail.bookingQuantity],
            totalDeliveredQuantity: [detail.totalDeliveredQuantity],
            remainingQuantity: [detail.remainingQuantity],
            billType: [detail.billType], // Add billType
            bookingRate: [detail.bookingRate], // Add bookingRate
            baseRate: [detail.baseRate], // Add baseRate for calculation
            bookingDate: [booking.bookingDate], // Add bookingDate for cycle calculation
            billingCycles: [0], // Number of billing cycles
            totalCharge: [0], // Will be calculated based on delivery quantity
            deliveryUnitId: [detail.bookingUnitId, Validators.required],
            deliveryQuantity: [
              null,
              [Validators.min(0), Validators.max(detail.remainingQuantity)],
            ], // No required, allow 0
            baseQuantity: [detail.baseQuantity],
            chargeAmount: [0, [Validators.min(0)]],
            labourCharge: [null, [Validators.min(0)]],
            availableUnits: [detail.availableUnits],
            convertedRemainingQty: [detail.remainingQuantity], // Initialize with booking unit remaining qty
          });

          // Calculate initial converted remaining quantity
          this.calculateConvertedRemainingQty(detailForm);

          // Watch for unit or quantity changes to calculate charge
          detailForm.get('deliveryQuantity')?.valueChanges.subscribe(() => {
            this.calculateBaseQuantity(detailForm);
            this.calculateItemCharge(detailForm);
            this.calculateTotalCharge();
          });

          detailForm.get('labourCharge')?.valueChanges.subscribe(() => {
            this.calculateTotalCharge();
          });

          detailForm.get('deliveryUnitId')?.valueChanges.subscribe(() => {
            this.calculateConvertedRemainingQty(detailForm);
            this.calculateBaseQuantity(detailForm);
            this.calculateItemCharge(detailForm);
            this.calculateTotalCharge();
          });

          this.deliveryDetails.push(detailForm);
        }
      });
      onReady?.();
    }, 50);

    // Calculate initial totals after all details are loaded
    setTimeout(() => {
      this.calculateTotalCharge();
    }, 0);
  }

  calculateBaseQuantity(detailForm: FormGroup) {
    const quantity = detailForm.get('deliveryQuantity')?.value || 0;
    const unitId = detailForm.get('deliveryUnitId')?.value;
    const units = detailForm.get('availableUnits')?.value || [];

    const selectedUnit = units.find((u: any) => u.id === unitId);
    if (selectedUnit) {
      const baseQty = quantity * selectedUnit.conversionRate;
      detailForm.patchValue({ baseQuantity: baseQty }, { emitEvent: false });
    }
  }

  calculateItemCharge(detailForm: FormGroup) {
    const deliveryQty = detailForm.get('deliveryQuantity')?.value || 0;
    const billType = detailForm.get('billType')?.value;
    const bookingRate = detailForm.get('bookingRate')?.value || 0;
    const bookingDate = detailForm.get('bookingDate')?.value;
    const deliveryDate = this.deliveryForm.get('deliveryDate')?.value;

    let totalCharge = 0;

    let cycleCount = 0;

    if (deliveryQty > 0 && bookingDate && deliveryDate) {
      // Calculate number of billing cycles
      cycleCount = this.calculateBillingCycles(
        bookingDate,
        deliveryDate,
        billType,
      );

      // Calculate charge: quantity × rate × cycle_count
      totalCharge = deliveryQty * bookingRate * cycleCount;
    }

    // Update the total charge and cycle count for this item
    detailForm.patchValue(
      {
        totalCharge: totalCharge,
        billingCycles: cycleCount,
      },
      { emitEvent: false },
    );
  }

  calculateBillingCycles(
    bookingDate: string | Date,
    deliveryDate: string | Date,
    billType: string,
  ): number {
    // Same rule as the server (RecurringChargeCalculator): booking on 01-Jul gives
    // cycle 1 = 01-Jul..01-Aug, cycle 2 = 02-Aug..01-Sep, ... The anniversary day still
    // belongs to the previous cycle; the next one starts the day after.
    const start = this.toLocalDay(bookingDate);
    const end = this.toLocalDay(deliveryDate);
    if (!start || !end || end <= start) {
      return 1; // same day (or delivery before booking) is the first cycle
    }

    const type = (billType || '').toUpperCase();
    const dayMs = 24 * 60 * 60 * 1000;
    const days = Math.round((end.getTime() - start.getTime()) / dayMs);

    if (type === 'HOURLY') return Math.max(1, days * 24);
    if (type === 'DAILY') return days + 1;

    // Upper-bound estimate of completed cycles, then step back until the cycle start is on/before the end date
    let completed =
      type === 'WEEKLY'
        ? Math.floor(days / 7)
        : type === 'YEARLY'
          ? end.getFullYear() - start.getFullYear()
          : (end.getFullYear() - start.getFullYear()) * 12 +
            (end.getMonth() - start.getMonth());
    while (completed > 0 && this.cycleStart(type, start, completed) > end) {
      completed--;
    }
    return Math.max(completed, 0) + 1;
  }

  /** Start of cycle k (k >= 1): the day after the k-th anniversary (month-end days clamp like .NET AddMonths). */
  private cycleStart(type: string, start: Date, k: number): Date {
    let anniversary: Date;
    if (type === 'WEEKLY') {
      anniversary = new Date(start.getFullYear(), start.getMonth(), start.getDate() + 7 * k);
    } else {
      const months = type === 'YEARLY' ? 12 * k : k;
      const target = new Date(start.getFullYear(), start.getMonth() + months, 1);
      const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
      anniversary = new Date(target.getFullYear(), target.getMonth(), Math.min(start.getDate(), lastDay));
    }
    return new Date(anniversary.getFullYear(), anniversary.getMonth(), anniversary.getDate() + 1);
  }

  /** Local calendar day (midnight) of an ISO date/time or a "yyyy-MM-dd" input value. */
  private toLocalDay(value: string | Date): Date | null {
    if (!value) return null;
    if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
      const [y, m, d] = value.split('-').map(Number);
      return new Date(y, m - 1, d);
    }
    const parsed = new Date(value);
    if (isNaN(parsed.getTime())) return null;
    return new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
  }

  calculateTotalCharge() {
    let totalRent = 0;
    let totalDeliveryLabour = 0;
    this.deliveryDetails.controls.forEach((control) => {
      const charge = control.get('totalCharge')?.value || 0;
      const labour = control.get('labourCharge')?.value || 0;
      totalRent += Number(charge);
      totalDeliveryLabour += Number(labour);
    });

    const grandTotal = totalRent + totalDeliveryLabour;
    this.deliveryForm.patchValue(
      {
        chargeAmount: grandTotal,
        //transactionAmount: grandTotal
      },
      { emitEvent: false },
    );
  }

  recalculateAllCharges() {
    // Recalculate charge for each delivery detail
    this.deliveryDetails.controls.forEach((control) => {
      this.calculateItemCharge(control as FormGroup);
    });
    // Then recalculate the total
    this.calculateTotalCharge();
  }

  onUnitChange(index: number) {
    const detail = this.deliveryDetails.at(index);
    this.calculateConvertedRemainingQty(detail as FormGroup);
  }

  calculateConvertedRemainingQty(detailForm: FormGroup) {
    const remainingQty = detailForm.get('remainingQuantity')?.value || 0;
    const bookingUnitId = detailForm.get('bookingUnitId')?.value;
    const deliveryUnitId = detailForm.get('deliveryUnitId')?.value;
    const units = detailForm.get('availableUnits')?.value || [];

    // Find the booking unit to get remaining quantity in base units
    const bookingUnit = units.find((u: any) => u.id === bookingUnitId);
    if (!bookingUnit) {
      detailForm.patchValue(
        { convertedRemainingQty: remainingQty },
        { emitEvent: false },
      );
      return;
    }

    // Calculate remaining quantity in base units
    const remainingBaseQty = remainingQty * bookingUnit.conversionRate;

    // Find the selected delivery unit
    const deliveryUnit = units.find((u: any) => u.id === deliveryUnitId);
    if (!deliveryUnit) {
      detailForm.patchValue(
        { convertedRemainingQty: remainingQty },
        { emitEvent: false },
      );
      return;
    }

    // Convert base quantity to delivery unit
    const convertedQty = remainingBaseQty / deliveryUnit.conversionRate;
    detailForm.patchValue(
      { convertedRemainingQty: convertedQty },
      { emitEvent: false },
    );
  }

  validateQuantity(index: number) {
    const detail = this.deliveryDetails.at(index);
    const units = detail.get('availableUnits')?.value || [];

    // Get remaining quantity in booking unit and convert to base
    const remainingQty = detail.get('remainingQuantity')?.value || 0;
    const bookingUnitId = detail.get('bookingUnitId')?.value;
    const bookingUnit = units.find((u: any) => u.id === bookingUnitId);
    const remainingBaseQty = bookingUnit
      ? remainingQty * bookingUnit.conversionRate
      : 0;

    // Get delivery quantity and convert to base
    const deliveryQty = detail.get('deliveryQuantity')?.value || 0;
    const deliveryUnitId = detail.get('deliveryUnitId')?.value;
    const deliveryUnit = units.find((u: any) => u.id === deliveryUnitId);
    const deliveryBaseQty = deliveryUnit
      ? deliveryQty * deliveryUnit.conversionRate
      : 0;

    if (deliveryBaseQty > remainingBaseQty) {
      const convertedRemainingQty =
        detail.get('convertedRemainingQty')?.value || 0;
      this.toastr.warning(
        `Delivery quantity cannot exceed remaining quantity (${convertedRemainingQty.toFixed(
          2,
        )})`,
      );

      detail.patchValue({ deliveryQuantity: convertedRemainingQty });
    }

    // Manually trigger recalculation since patchValue might not trigger subscription
    this.calculateBaseQuantity(detail as FormGroup);
    this.calculateItemCharge(detail as FormGroup);
    this.calculateTotalCharge();
  }

  getBillTypeLabel(billType: string): string {
    const labels: { [key: string]: string } = {
      HOURLY: 'Hourly',
      DAILY: 'Daily',
      WEEKLY: 'Weekly',
      MONTHLY: 'Monthly',
      YEARLY: 'Yearly',
    };
    return labels[billType] || billType;
  }

  getBillTypeCycleLabel(billType: string): string {
    const labels: { [key: string]: string } = {
      HOURLY: 'hr(s)',
      DAILY: 'day(s)',
      WEEKLY: 'week(s)',
      MONTHLY: 'month(s)',
      YEARLY: 'year(s)',
    };
    return labels[billType] || 'cycle(s)';
  }

  onSubmit() {
    // Validate form - only check booking is selected
    if (!this.deliveryForm.get('bookingId')?.value) {
      this.toastr.error('Please select a booking');
      return;
    }

    if (this.deliveryDetails.length === 0) {
      this.toastr.error('No items available for delivery');
      return;
    }

    // Check if at least one item has quantity > 0
    const hasDeliveryQuantity = this.deliveryDetails.controls.some(
      (control) => {
        const deliveryQty = control.get('deliveryQuantity')?.value || 0;
        return deliveryQty > 0;
      },
    );

    if (!hasDeliveryQuantity) {
      this.toastr.error('Please enter delivery quantity for at least one item');
      return;
    }

    // Check if all remaining quantities will be zero (full delivery completed)
    const allRemainingWillBeZero = this.deliveryDetails.controls
      .filter((control) => (control.get('deliveryQuantity')?.value || 0) > 0)
      .every((control) => {
        const deliveryQty = control.get('deliveryQuantity')?.value || 0;
        const remainingQty = control.get('remainingQuantity')?.value || 0;
        return remainingQty - deliveryQty === 0;
      });

    // Validate quantities using base units and calculate base quantity (skip items with zero delivery qty)
    for (let i = 0; i < this.deliveryDetails.controls.length; i++) {
      const control = this.deliveryDetails.at(i);
      const deliveryQty = control.get('deliveryQuantity')?.value || 0;

      if (deliveryQty > 0) {
        const units = control.get('availableUnits')?.value || [];

        // Get remaining quantity in booking unit and convert to base
        const remainingQty = control.get('remainingQuantity')?.value || 0;
        const bookingUnitId = control.get('bookingUnitId')?.value;
        const bookingUnit = units.find((u: any) => u.id === bookingUnitId);
        const remainingBaseQty = bookingUnit
          ? remainingQty * bookingUnit.conversionRate
          : 0;

        // Get delivery quantity and convert to base
        const deliveryUnitId = control.get('deliveryUnitId')?.value;
        const deliveryUnit = units.find((u: any) => u.id === deliveryUnitId);
        const deliveryBaseQty = deliveryUnit
          ? deliveryQty * deliveryUnit.conversionRate
          : 0;

        // Update baseQuantity field for payload
        control.patchValue(
          { baseQuantity: deliveryBaseQty },
          { emitEvent: false },
        );

        // Validate that delivery base quantity doesn't exceed remaining base quantity
        if (deliveryBaseQty > remainingBaseQty) {
          const convertedRemainingQty =
            control.get('convertedRemainingQty')?.value || 0;
          this.toastr.error(
            `Item ${i + 1
            }: Delivery quantity (${deliveryQty}) cannot exceed remaining quantity (${convertedRemainingQty.toFixed(
              2,
            )})`,
          );
          return;
        }
      }
    }

    const txAmount = this.isContractBooking
      ? 0
      : Number(this.deliveryForm.get('transactionAmount')?.value) || 0;
    const chargeAmount = Number(this.deliveryForm.get('chargeAmount')?.value) || 0;

    if (txAmount < 0) {
      this.toastr.error('Collection amount cannot be negative');
      return;
    }

    if (txAmount > chargeAmount + 0.01) {
      this.toastr.error(
        `Collection amount (৳${txAmount.toFixed(2)}) cannot exceed delivery total (৳${chargeAmount.toFixed(2)})`,
      );
      return;
    }

    const formData = this.deliveryForm.value;

    // If amount is not set (<= 0) or less than total charge, show confirmation
    // (not for contract bookings: they are never paid at delivery)
    if (!this.isContractBooking && txAmount < chargeAmount - 0.01) {
      let confirmHtml = '';

      if (txAmount <= 0) {
        confirmHtml = `
          <div style="font-family: inherit; text-align: start; padding: 6px 2px 2px;">
            <!-- Header Icon & Title -->
            <div style="text-align: center; margin-bottom: 20px;">
              <div style="width: 58px; height: 58px; border-radius: 50%; background: #FEF2F2; border: 2px solid #FEE2E2; color: #DC2626; display: inline-flex; align-items: center; justify-content: center; font-size: 24px; box-shadow: 0 4px 14px rgba(220, 38, 38, 0.12);">
                <i class="fas fa-exclamation-triangle"></i>
              </div>
              <h4 style="margin: 14px 0 4px; font-weight: 700; color: #0F172A; font-size: 19px; letter-spacing: -0.01em;">Confirm Unpaid Delivery</h4>
              <p style="margin: 0; color: #64748B; font-size: 13px;">No payment will be collected for this delivery</p>
            </div>

            <!-- Summary Card -->
            <div style="background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 12px; padding: 14px 18px; margin-bottom: 16px;">
              <div style="display: flex; justify-content: space-between; align-items: center; padding-bottom: 10px; border-bottom: 1px solid #EDF2F7;">
                <span style="color: #475569; font-size: 13px; font-weight: 500; display: inline-flex; align-items: center;">
                  <i class="fas fa-file-invoice-dollar me-2" style="color: #64748B; font-size: 14px;"></i>Total Delivery Bill
                </span>
                <span style="color: #0F172A; font-weight: 700; font-size: 15px;">
                  ৳${chargeAmount.toFixed(2)}
                </span>
              </div>

              <div style="display: flex; justify-content: space-between; align-items: center; padding: 10px 0; border-bottom: 1px solid #EDF2F7;">
                <span style="color: #475569; font-size: 13px; font-weight: 500; display: inline-flex; align-items: center;">
                  <i class="fas fa-coins me-2" style="color: #94A3B8; font-size: 14px;"></i>Collected Amount
                </span>
                <span style="background: #F1F5F9; color: #64748B; font-weight: 600; font-size: 12px; padding: 3px 8px; border-radius: 6px;">
                  ৳0.00 (None)
                </span>
              </div>

              <div style="display: flex; justify-content: space-between; align-items: center; padding-top: 10px;">
                <span style="color: #B91C1C; font-size: 13px; font-weight: 600; display: inline-flex; align-items: center;">
                  <i class="fas fa-exclamation-circle me-2" style="color: #DC2626; font-size: 14px;"></i>Remaining Customer Due
                </span>
                <span style="color: #DC2626; font-weight: 800; font-size: 16px;">
                  ৳${chargeAmount.toFixed(2)}
                </span>
              </div>
            </div>

            <!-- Warning Notice Box -->
            <div style="background: #FFFBEB; border: 1px solid #FDE68A; border-radius: 10px; padding: 12px 14px; margin-bottom: 14px; display: flex; align-items: flex-start; text-align: left;">
              <i class="fas fa-info-circle me-2 mt-1" style="color: #D97706; font-size: 14px; flex-shrink: 0;"></i>
              <div style="color: #92400E; font-size: 12.5px; line-height: 1.5;">
                No payment is being collected now. The entire <strong>৳${chargeAmount.toFixed(2)}</strong> will be recorded as outstanding customer due.
              </div>
            </div>

            <p style="color: #475569; font-size: 13px; font-weight: 500; text-align: center; margin: 4px 0 0;">
              Are you sure you want to complete this delivery without payment?
            </p>
          </div>
        `;
      } else {
        const remainingDue = chargeAmount - txAmount;
        confirmHtml = `
          <div style="font-family: inherit; text-align: start; padding: 6px 2px 2px;">
            <!-- Header Icon & Title -->
            <div style="text-align: center; margin-bottom: 20px;">
              <div style="width: 58px; height: 58px; border-radius: 50%; background: #EFF6FF; border: 2px solid #DBEAFE; color: #2563EB; display: inline-flex; align-items: center; justify-content: center; font-size: 24px; box-shadow: 0 4px 14px rgba(37, 99, 235, 0.12);">
                <i class="fas fa-hand-holding-usd"></i>
              </div>
              <h4 style="margin: 14px 0 4px; font-weight: 700; color: #0F172A; font-size: 19px; letter-spacing: -0.01em;">Confirm Partial Payment</h4>
              <p style="margin: 0; color: #64748B; font-size: 13px;">Review the payment summary before completing delivery</p>
            </div>

            <!-- Summary Card -->
            <div style="background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 12px; padding: 14px 18px; margin-bottom: 16px;">
              <div style="display: flex; justify-content: space-between; align-items: center; padding-bottom: 10px; border-bottom: 1px solid #EDF2F7;">
                <span style="color: #475569; font-size: 13px; font-weight: 500; display: inline-flex; align-items: center;">
                  <i class="fas fa-file-invoice-dollar me-2" style="color: #64748B; font-size: 14px;"></i>Total Delivery Bill
                </span>
                <span style="color: #0F172A; font-weight: 700; font-size: 15px;">
                  ৳${chargeAmount.toFixed(2)}
                </span>
              </div>

              <div style="display: flex; justify-content: space-between; align-items: center; padding: 10px 0; border-bottom: 1px solid #EDF2F7;">
                <span style="color: #475569; font-size: 13px; font-weight: 500; display: inline-flex; align-items: center;">
                  <i class="fas fa-coins me-2" style="color: #10B981; font-size: 14px;"></i>Collecting Now
                </span>
                <span style="color: #059669; font-weight: 700; font-size: 15px;">
                  ৳${txAmount.toFixed(2)}
                </span>
              </div>

              <div style="display: flex; justify-content: space-between; align-items: center; padding-top: 10px;">
                <span style="color: #B91C1C; font-size: 13px; font-weight: 600; display: inline-flex; align-items: center;">
                  <i class="fas fa-exclamation-circle me-2" style="color: #DC2626; font-size: 14px;"></i>Remaining Customer Due
                </span>
                <span style="color: #DC2626; font-weight: 800; font-size: 16px;">
                  ৳${remainingDue.toFixed(2)}
                </span>
              </div>
            </div>

            <!-- Soft Info Box -->
            <div style="background: #EFF6FF; border: 1px solid #BFDBFE; border-radius: 10px; padding: 12px 14px; margin-bottom: 14px; display: flex; align-items: flex-start; text-align: left;">
              <i class="fas fa-info-circle me-2 mt-1" style="color: #2563EB; font-size: 14px; flex-shrink: 0;"></i>
              <div style="color: #1E40AF; font-size: 12.5px; line-height: 1.5;">
                Partial payment recorded. The remaining <strong>৳${remainingDue.toFixed(2)}</strong> will remain unpaid on the customer's balance.
              </div>
            </div>

            <p style="color: #475569; font-size: 13px; font-weight: 500; text-align: center; margin: 4px 0 0;">
              Are you sure you want to proceed with this partial payment?
            </p>
          </div>
        `;
      }

      Swal.fire({
        html: confirmHtml,
        showCancelButton: true,
        buttonsStyling: false,
        width: '460px',
        background: '#FFFFFF',
        padding: '24px 20px',
        customClass: {
          popup: 'rounded-4 shadow-lg border-0',
          htmlContainer: 'p-0 m-0',
          actions: 'd-flex justify-content-center gap-3 mt-3 mb-1 w-100',
          confirmButton: 'btn btn-primary px-4 py-2 fw-semibold rounded-2 shadow-sm d-inline-flex align-items-center',
          cancelButton: 'btn btn-outline-secondary px-4 py-2 fw-semibold rounded-2 d-inline-flex align-items-center',
        },
        confirmButtonText: '<i class="fas fa-check-circle me-2"></i> Yes, Proceed',
        cancelButtonText: '<i class="fas fa-times me-2"></i> No, Cancel',
      }).then((result) => {
        if (result.value) {
          this.submitDelivery(formData);
        } else {
          this.shouldAutoPrint = false;
        }
      });
    } else {
      this.submitDelivery(formData);
    }
  }

  submitDelivery(formData: any) {
    this.isSubmitting = true;
    const txAmount = this.isContractBooking ? 0 : Number(formData.transactionAmount) || 0;
    const hasPayment = txAmount > 0;
    const id = this.route.snapshot.paramMap.get('id');

    // Prepare payload (filter out zero quantity items)
    const payload: IDeliveryRequest = {
      deliveryNumber: formData.deliveryNumber,
      deliveryDate: formData.deliveryDate,
      bookingId: formData.bookingId,
      notes: formData.notes,
      chargeAmount: formData.chargeAmount,
      adjustmentValue: 0, // Always 0 as per requirement
      deliveryDetails: formData.deliveryDetails
        .filter((d: any) => (d.deliveryQuantity || 0) > 0) // Only include items with qty > 0
        .map((d: any) => ({
          bookingDetailId: d.bookingDetailId,
          deliveryUnitId: d.deliveryUnitId,
          deliveryQuantity: d.deliveryQuantity,
          baseQuantity: d.baseQuantity,
          billingCycles: d.billingCycles || 1,
          chargeAmount: d.totalCharge || 0, // Use totalCharge which is calculated
          labourCharge: d.labourCharge || 0,
          adjustmentValue: 0,
        })),
      createTransaction: hasPayment,
      // On edit always send the amount: 0 removes the delivery-time payment
      // Contract bookings: send nothing so an amount taken before the booking became a contract is kept
      transactionAmount: this.isContractBooking
        ? undefined
        : hasPayment || id
          ? txAmount
          : undefined,
      paymentMethod: hasPayment ? formData.paymentMethod || 'CASH' : undefined,
      bankId: hasPayment && formData.paymentMethod !== 'CASH' ? formData.bankId : undefined,
      paymentReference: hasPayment && formData.paymentMethod !== 'CASH' ? formData.paymentReference : undefined,
      transactionNotes: formData.transactionNotes || undefined,
    };

    const action = id
      ? this.deliveryService.update(id, payload)
      : this.deliveryService.create(payload);

    action.subscribe({
      next: (response: any) => {
        const isEditMode = !!id;
        this.toastr.success(
          `Delivery ${isEditMode ? 'updated' : 'created'} successfully`,
        );
        this.isSubmitting = false;

        // Handle printing if shouldAutoPrint is true
        if (this.shouldAutoPrint) {
          const deliveryId = isEditMode ? id : response?.id;
          if (deliveryId) {
            this.loadInvoiceForPrint(deliveryId);
          }
        } else {
          // For regular save without print
          if (isEditMode) {
            // In edit mode, stay on page or navigate to list
            this.router.navigate(['/product-delivery/list']);
          } else {
            // In create mode, reset form
            this.reset();
          }
        }
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to save delivery');
        this.isSubmitting = false;
        this.shouldAutoPrint = false;
      },
    });
  }

  loadExistingDelivery(id: string) {
    this.isLoading = true;
    this.deliveryService.getById(id).subscribe({
      next: (delivery) => {
        if (!delivery.bookingId) {
          this.toastr.error('Delivery has no associated booking');
          this.isLoading = false;
          return;
        }

        this.deliveryService.getBookingForDelivery(delivery.bookingId).subscribe({
          next: (booking) => {
            // Remaining quantities from the server already subtract this delivery,
            // so add its own quantities back before building the editable rows.
            booking.bookingDetails.forEach((bd) => {
              const ownBaseQty = delivery.deliveryDetails
                .filter((dd) => dd.bookingDetailId === bd.id)
                .reduce((sum, dd) => sum + (dd.baseQuantity || 0), 0);
              if (ownBaseQty > 0 && bd.bookingQuantity > 0) {
                const bookingUnitFactor = bd.baseQuantity / bd.bookingQuantity || 1;
                const ownQty = ownBaseQty / bookingUnitFactor;
                bd.remainingQuantity += ownQty;
                bd.totalDeliveredQuantity -= ownQty;
              }
            });

            this.bookingData = booking;

            this.deliveryForm.patchValue(
              {
                deliveryNumber: delivery.deliveryNumber,
                deliveryDate: new Date(delivery.deliveryDate)
                  .toISOString()
                  .split('T')[0],
                bookingId: delivery.bookingId,
                notes: delivery.notes || '',
                chargeAmount: delivery.chargeAmount,
                transactionAmount: delivery.collectedAmount || null,
                paymentMethod: delivery.paymentMethod || 'CASH',
                bankId: delivery.bankId ?? null,
                paymentReference: delivery.paymentReference || '',
                transactionNotes: delivery.transactionNotes || '',
              },
              { emitEvent: false },
            );
            this.deliveryNumber = delivery.deliveryNumber;

            // Rows are built asynchronously; patch the saved quantities once they exist
            this.populateDeliveryDetails(booking, () => {
              delivery.deliveryDetails.forEach((detail) => {
                const index = this.deliveryDetails.controls.findIndex(
                  (ctrl) =>
                    ctrl.get('bookingDetailId')?.value ===
                    detail.bookingDetailId,
                );
                if (index === -1) return;

                const detailForm = this.deliveryDetails.at(index) as FormGroup;
                detailForm.patchValue(
                  {
                    deliveryUnitId: detail.deliveryUnitId,
                    deliveryQuantity: detail.deliveryQuantity,
                    billingCycles: detail.billingCycles || 0,
                    totalCharge: detail.chargeAmount,
                    labourCharge: detail.labourCharge ?? null,
                    baseQuantity: detail.baseQuantity,
                    bookingRate:
                      detail.bookingRate || detailForm.get('bookingRate')?.value,
                    billType: detail.billType || detailForm.get('billType')?.value,
                  },
                  { emitEvent: false },
                );

                this.calculateConvertedRemainingQty(detailForm);
                this.calculateBaseQuantity(detailForm);
              });

              this.calculateTotalCharge();
              this.isLoading = false;
            });
          },
          error: () => {
            this.toastr.error('Failed to load booking details');
            this.isLoading = false;
          },
        });
      },
      error: () => {
        this.toastr.error('Failed to load delivery');
        this.isLoading = false;
      },
    });
  }

  onSaveAndPrint() {
    if (this.deliveryForm.valid) {
      this.shouldAutoPrint = true;
      this.onSubmit();
    }
  }

  loadInvoiceForPrint(deliveryId: string) {
    this.invoiceId = deliveryId;
    this.showInvoice = true;

    // Trigger print after a short delay to allow component to load
    setTimeout(() => {
      if (this.invoiceComponent) {
        this.invoiceComponent.triggerPrint();
      }
      // Check if we're in edit mode
      const isEditMode = this.route.snapshot.paramMap.get('id');
      if (!isEditMode) {
        // Only reset form if not in edit mode
        this.reset();
      } else {
        // In edit mode, just clear the print flags
        this.showInvoice = false;
        this.invoiceId = '';
        this.shouldAutoPrint = false;
      }
    }, 500);
  }

  reset() {
    this.bookingData = null;
    this.showInvoice = false;
    this.invoiceId = '';
    this.shouldAutoPrint = false;
    this.initForm();
    this.generateDeliveryNumber();
    this.loadBookingLookup();
  }
}
