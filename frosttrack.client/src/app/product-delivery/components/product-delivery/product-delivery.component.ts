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
import {
  IBookingForDeliveryResponse,
  IDeliveryRequest,
} from 'app/delivery/models/delivery.interface';
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

  // For printing
  invoiceId: string = '';
  showInvoice: boolean = false;
  shouldAutoPrint: boolean = false;

  paymentMethods = [
    { value: 'CASH', label: 'Cash' },
    { value: 'BANK_TRANSFER', label: 'Bank Transfer' },
    { value: 'CHEQUE', label: 'Cheque' },
    { value: 'MOBILE_BANKING', label: 'Mobile Banking' },
  ];

  constructor(
    private fb: FormBuilder,
    private deliveryService: DeliveryService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit() {
    this.initForm();
    this.generateDeliveryNumber();
    this.loadBookingLookup();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.loadExistingDelivery(id);
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
      transactionNotes: [''],
    });

    // Watch for booking changes
    this.deliveryForm.get('bookingId')?.valueChanges.subscribe((bookingId) => {
      if (bookingId) {
        this.onBookingChange(bookingId);
      }
    });

    // Watch for delivery date changes to recalculate billing cycles
    this.deliveryForm.get('deliveryDate')?.valueChanges.subscribe(() => {
      this.recalculateAllCharges();
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

          // Fetch booking due amount
          this.fetchBookingDue(bookingId);

          this.isLoading = false;
        },
        error: (err) => {
          this.isLoading = false;
          this.toastr.error(err.error?.message || 'Booking not found');
        },
      });
  }

  fetchBookingDue(bookingId: string) {
    this.deliveryService.getBookingDueAmount(bookingId).subscribe({
      next: (dueAmount) => {
        this.deliveryForm.patchValue(
          { totalPreviousPayments: dueAmount },
          { emitEvent: false },
        );
      },
      error: () => {
        this.deliveryForm.patchValue(
          { totalPreviousPayments: 0 },
          { emitEvent: false },
        );
      },
    });
  }

  populateDeliveryDetails(booking: IBookingForDeliveryResponse) {
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
    const start = new Date(bookingDate);
    const end = new Date(deliveryDate);

    // Calculate the difference in milliseconds
    const diffTime = end.getTime() - start.getTime();

    if (diffTime < 0) {
      return 1; // Delivery before booking
    }

    let cycles = 0;

    switch (billType) {
      case 'HOURLY':
        // Number of hours
        cycles = Math.ceil(diffTime / (1000 * 60 * 60));
        break;

      case 'DAILY':
        // Number of days
        cycles = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        break;

      case 'WEEKLY':
        // Number of weeks
        cycles = Math.ceil(diffTime / (1000 * 60 * 60 * 24 * 7));
        break;

      case 'MONTHLY': {
        // Calculate months difference
        let months =
          (end.getFullYear() - start.getFullYear()) * 12 +
          (end.getMonth() - start.getMonth());

        // If we're in a new month but haven't passed the booking day, don't count it yet
        if (end.getDate() < start.getDate()) {
          months--;
        }

        // Add 1 to include the first month
        cycles = months + 1;
        break;
      }

      case 'YEARLY': {
        // Calculate years difference
        let years = end.getFullYear() - start.getFullYear();

        // If we're in a new year but haven't passed the booking month/day, don't count it yet
        if (
          end.getMonth() < start.getMonth() ||
          (end.getMonth() === start.getMonth() &&
            end.getDate() < start.getDate())
        ) {
          years--;
        }

        // Add 1 to include the first year
        cycles = years + 1;
        break;
      }

      default:
        cycles = 1; // Default to 1 cycle
    }

    return Math.max(cycles, 1); // At least 1 cycle
  }

  calculateTotalCharge() {
    let total = 0;
    let totalLabour = 0;
    this.deliveryDetails.controls.forEach((control) => {
      const charge = control.get('totalCharge')?.value || 0;
      const labour = control.get('labourCharge')?.value || 0;
      total += Number(charge);
      totalLabour += Number(labour);
    });

    const grandTotal = total + totalLabour;
    this.deliveryForm.patchValue(
      { chargeAmount: grandTotal },
      { emitEvent: false },
    );

    // Update transaction amount with grand total (charge + labour) if transaction is enabled
    if (this.deliveryForm.get('createTransaction')?.value) {
      this.deliveryForm.patchValue(
        { transactionAmount: grandTotal > 0 ? grandTotal : 0 },
        { emitEvent: false },
      );
    }
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
            `Item ${
              i + 1
            }: Delivery quantity (${deliveryQty}) cannot exceed remaining quantity (${convertedRemainingQty.toFixed(
              2,
            )})`,
          );
          return;
        }
      }
    }
    // If all remaining quantities will be zero and no payment, show confirmation
    const formData = this.deliveryForm.value;
    const isShowConfirm =
      allRemainingWillBeZero &&
      !this.deliveryForm.get('createTransaction')?.value;

    if (isShowConfirm) {
      Swal.fire({
        title: 'Confirmation',
        text: 'This delivery will complete all remaining quantities without collecting payment. Are you sure?',
        showCancelButton: true,
        confirmButtonColor: SwalConfirm.confirmButtonColor,
        cancelButtonColor: SwalConfirm.cancelButtonColor,
        confirmButtonText: 'Yes',
        cancelButtonText: 'No',
      }).then((result) => {
        if (result.value) {
          this.submitDelivery(formData);
        }
      });
    } else {
      this.submitDelivery(formData);
    }
  }

  submitDelivery(formData: any) {
    this.isSubmitting = true;
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
      createTransaction: formData.createTransaction,
      transactionAmount: formData.createTransaction
        ? formData.transactionAmount
        : undefined,
      paymentMethod: formData.createTransaction
        ? formData.paymentMethod
        : undefined,
      transactionNotes: formData.createTransaction
        ? formData.transactionNotes
        : undefined,
    };

    const id = this.route.snapshot.paramMap.get('id');
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
    // Implementation for loading existing delivery for edit
    this.isLoading = true;
    this.deliveryService.getById(id).subscribe({
      next: (delivery) => {
        // First load booking to get full details with remaining quantities
        if (delivery.bookingId) {
          const selectedBooking = this.bookings.find(
            (b) => b.value === delivery.bookingId,
          );
          if (selectedBooking) {
            this.deliveryService
              .getBookingForDelivery(selectedBooking.value)
              .subscribe({
                next: (booking) => {
                  this.bookingData = booking;

                  // Populate delivery details from booking
                  this.populateDeliveryDetails(booking);

                  // Now patch the form with delivery values
                  this.deliveryForm.patchValue(
                    {
                      deliveryNumber: delivery.deliveryNumber,
                      deliveryDate: new Date(delivery.deliveryDate)
                        .toISOString()
                        .split('T')[0],
                      bookingId: delivery.bookingId,
                      notes: delivery.notes || '',
                      chargeAmount: delivery.chargeAmount,
                    },
                    { emitEvent: false },
                  );

                  // Populate delivery quantities from existing delivery
                  delivery.deliveryDetails.forEach((detail) => {
                    const index = this.deliveryDetails.controls.findIndex(
                      (ctrl) =>
                        ctrl.get('bookingDetailId')?.value ===
                        detail.bookingDetailId,
                    );

                    if (index !== -1) {
                      const detailForm = this.deliveryDetails.at(
                        index,
                      ) as FormGroup;

                      // Use the billing cycles and rates from the saved delivery
                      detailForm.patchValue(
                        {
                          deliveryUnitId: detail.deliveryUnitId,
                          deliveryQuantity: detail.deliveryQuantity,
                          billingCycles: detail.billingCycles || 0,
                          totalCharge: detail.chargeAmount,
                          baseQuantity: detail.baseQuantity,
                          bookingRate:
                            detail.bookingRate ||
                            detailForm.get('bookingRate')?.value,
                          billType:
                            detail.billType ||
                            detailForm.get('billType')?.value,
                        },
                        { emitEvent: false },
                      );

                      // Recalculate converted remaining quantity for selected unit
                      this.calculateConvertedRemainingQty(detailForm);
                      this.calculateBaseQuantity(detailForm);
                    }
                  });

                  // Recalculate total charge to ensure sum is correct
                  this.calculateTotalCharge();

                  // Fetch booking due amount
                  this.fetchBookingDue(delivery.bookingId);

                  this.isLoading = false;
                },
                error: () => {
                  this.toastr.error('Failed to load booking details');
                  this.isLoading = false;
                },
              });
          } else {
            this.toastr.error('Booking not found in lookup');
            this.isLoading = false;
          }
        } else {
          this.toastr.error('Delivery has no associated booking');
          this.isLoading = false;
        }
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
