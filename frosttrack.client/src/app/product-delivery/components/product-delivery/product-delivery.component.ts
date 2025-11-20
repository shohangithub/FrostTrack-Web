import { Component, OnInit } from '@angular/core';
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
import { MessageHub } from '@config/message-hub';
import { SwalConfirm } from 'app/theme-config';

@Component({
  selector: 'app-delivery',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgSelectModule],
  templateUrl: './product-delivery.component.html',
})
export class DeliveryComponent implements OnInit {
  deliveryForm!: FormGroup;
  bookingData: IBookingForDeliveryResponse | null = null;
  bookings: { value: string; text: string }[] = [];
  bookingLoading = false;
  isLoading = false;
  isSubmitting = false;
  deliveryNumber = '';

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
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.initForm();
    this.generateDeliveryNumber();
    this.loadBookingLookup();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
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
      createTransaction: [false],
      transactionAmount: [
        null,
        [
          Validators.min(0),
          Validators.max(
            this.deliveryForm?.get('remainingBalance')?.value || 0
          ),
        ],
      ],
      paymentMethod: ['CASH'],
      transactionNotes: [''],
    });

    // Watch for booking changes
    this.deliveryForm.get('bookingId')?.valueChanges.subscribe((bookingId) => {
      if (bookingId) {
        this.onBookingChange(bookingId);
      }
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
    this.deliveryService.getBookingForDelivery(selectedBooking.text).subscribe({
      next: (booking) => {
        this.bookingData = booking;
        this.populateDeliveryDetails(booking);

        // Fetch previous payments for this booking
        this.fetchPreviousPayments(bookingId);

        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.toastr.error(err.error?.message || 'Booking not found');
      },
    });
  }

  fetchPreviousPayments(bookingId: string) {
    this.deliveryService.getBookingPreviousPayments(bookingId).subscribe({
      next: (totalPaid) => {
        this.deliveryForm.patchValue(
          { totalPreviousPayments: totalPaid },
          { emitEvent: false }
        );
        // Recalculate after updating previous payments
        this.calculateTotalCharge();
      },
      error: () => {
        // If fetching fails, default to 0
        this.deliveryForm.patchValue(
          { totalPreviousPayments: 0 },
          { emitEvent: false }
        );
        this.calculateTotalCharge();
      },
    });
  }

  populateDeliveryDetails(booking: IBookingForDeliveryResponse) {
    this.deliveryDetails.clear();

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
          totalCharge: [detail.totalCharge],
          deliveryUnitId: [detail.bookingUnitId, Validators.required],
          deliveryQuantity: [
            null,
            [Validators.min(0), Validators.max(detail.remainingQuantity)],
          ], // No required, allow 0
          baseQuantity: [0],
          chargeAmount: [0, [Validators.min(0)]],
          availableUnits: [detail.availableUnits],
        });

        // // Watch for unit or quantity changes to calculate base quantity and charge
        // detailForm.get('deliveryQuantity')?.valueChanges.subscribe(() => {
        //   this.calculateBaseQuantity(detailForm);
        //   this.calculateItemCharge(detailForm);
        //   this.calculateTotalCharge();
        // });

        // detailForm.get('deliveryUnitId')?.valueChanges.subscribe(() => {
        //   this.calculateBaseQuantity(detailForm);
        // });

        this.deliveryDetails.push(detailForm);
      }
    });
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

  // calculateItemCharge(detailForm: FormGroup) {
  //   // const quantity = detailForm.get('deliveryQuantity')?.value || 0;
  //   // const chargePerUnit = detailForm.get('chargePerUnit')?.value || 0;
  //   // const totalCharge = quantity * chargePerUnit;
  //   const totalCharge = detailForm.get('totalCharge')?.value || 0;
  //   detailForm.patchValue({ chargeAmount: totalCharge }, { emitEvent: false });
  // }

  calculateTotalCharge() {
    let total = 0;
    this.deliveryDetails.controls.forEach((control) => {
      const charge = control.get('totalCharge')?.value || 0;
      total += charge;
    });

    this.deliveryForm.patchValue({ chargeAmount: total }, { emitEvent: false });

    // Calculate remaining balance: total - previous payments
    const previousPayments =
      this.deliveryForm.get('totalPreviousPayments')?.value || 0;
    const remainingBalance = total - previousPayments;

    this.deliveryForm.patchValue(
      { remainingBalance: remainingBalance },
      { emitEvent: false }
    );

    // Update transaction amount with remaining balance if transaction is enabled
    if (this.deliveryForm.get('createTransaction')?.value) {
      this.deliveryForm.patchValue(
        { transactionAmount: remainingBalance > 0 ? remainingBalance : 0 },
        { emitEvent: false }
      );
    }
  }

  validateQuantity(index: number) {
    const detail = this.deliveryDetails.at(index);
    const deliveryQty = detail.get('deliveryQuantity')?.value || 0;
    const remainingQty = detail.get('remainingQuantity')?.value || 0;

    if (deliveryQty > remainingQty) {
      this.toastr.warning(
        `Delivery quantity cannot exceed remaining quantity (${remainingQty})`
      );
      detail.patchValue({ deliveryQuantity: remainingQty });
    }
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
      }
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

    // Validate quantities (skip items with zero delivery qty)
    let hasInvalidQty = false;
    this.deliveryDetails.controls.forEach((control, index) => {
      const qty = control.get('deliveryQuantity')?.value || 0;
      const remainingQty = control.get('remainingQuantity')?.value || 0;

      if (qty > 0 && qty > remainingQty) {
        hasInvalidQty = true;
        this.toastr.error(
          `Item ${
            index + 1
          }: Delivery quantity cannot exceed remaining quantity (${remainingQty})`
        );
      }
    });

    if (hasInvalidQty) return;
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
          chargeAmount: d.chargeAmount,
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
      next: () => {
        this.toastr.success(
          `Delivery ${id ? 'updated' : 'created'} successfully`
        );
        this.router.navigate(['/product-delivery/list']);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to save delivery');
        this.isSubmitting = false;
      },
    });
  }

  loadExistingDelivery(id: string) {
    // Implementation for loading existing delivery for edit
    this.isLoading = true;
    this.deliveryService.getById(id).subscribe({
      next: (delivery) => {
        // Load and populate form
        this.deliveryForm.patchValue({
          deliveryNumber: delivery.deliveryNumber,
          deliveryDate: delivery.deliveryDate,
          bookingId: delivery.bookingId,
          notes: delivery.notes,
          chargeAmount: delivery.chargeAmount,
        });

        // Load booking details
        if (delivery.bookingId) {
          this.deliveryForm.patchValue({ bookingId: delivery.bookingId });
          this.onBookingChange(delivery.bookingId);
        }

        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load delivery');
        this.isLoading = false;
      },
    });
  }

  reset() {
    this.bookingData = null;
    this.initForm();
    this.generateDeliveryNumber();
    this.loadBookingLookup();
  }
}
