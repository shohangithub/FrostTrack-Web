import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  FormsModule,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import {
  ITransactionRequest,
  TransactionType,
  TransactionFlow,
} from 'app/transaction/models/transaction.interface';
import { BillCollectionService } from '../../services/bill-collection.service';
import { IBookingWithDueResponse } from '../../models/bill-collection.interface';

@Component({
  selector: 'app-bill-collection',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    RouterLink,
  ],
  templateUrl: './bill-collection.component.html',
})
export class BillCollectionComponent implements OnInit {
  billCollectionForm!: FormGroup;
  bookings: { value: string; text: string }[] = [];
  selectedBooking: IBookingWithDueResponse | null = null;
  bookingLoading = false;
  isLoading = false;
  isSubmitting = false;
  isEditing = false;
  transactionCode = '';
  selectedBranch!: number;

  paymentMethods = [
    { value: 'CASH', label: 'Cash' },
    { value: 'BANK_TRANSFER', label: 'Bank Transfer' },
    { value: 'CHEQUE', label: 'Cheque' },
    { value: 'MOBILE_BANKING', label: 'Mobile Banking' },
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();
    this.loadBookingsWithDue();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditing = true;
      this.loadExistingTransaction(id);
    } else {
      this.generateTransactionCode();
    }
  }

  initForm() {
    this.billCollectionForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      transactionCode: ['', Validators.required],
      transactionDate: [
        new Date().toISOString().split('T')[0],
        Validators.required,
      ],
      bookingId: ['', Validators.required],
      branchId: [this.selectedBranch, Validators.required],
      amount: [null, [Validators.required, Validators.min(0)]],
      paymentMethod: ['CASH', Validators.required],
      note: [''],
    });

    // Watch for booking changes
    this.billCollectionForm
      .get('bookingId')
      ?.valueChanges.subscribe((bookingId) => {
        if (bookingId) {
          this.onBookingChange(bookingId);
        }
      });
  }

  generateTransactionCode() {
    this.transactionService.generateCode().subscribe({
      next: (response) => {
        this.transactionCode = response.code;
        this.billCollectionForm.patchValue({ transactionCode: response.code });
      },
      error: () => {
        this.toastr.error('Failed to generate transaction code');
      },
    });
  }

  loadBookingsWithDue() {
    this.bookingLoading = true;
    this.billCollectionService.getBookingsWithDue().subscribe({
      next: (bookings) => {
        this.bookings = bookings;
        this.bookingLoading = false;
        if (bookings.length === 0) {
          this.toastr.info('No bookings with outstanding dues found');
        }
      },
      error: (err) => {
        console.error('Failed to load bookings:', err);
        const errorMessage =
          err?.error?.message ||
          err?.message ||
          'Failed to load bookings with due';
        this.toastr.error(errorMessage);
        this.bookingLoading = false;
      },
    });
  }

  onBookingChange(bookingId: string) {
    if (!bookingId) return;

    this.isLoading = true;
    this.billCollectionService
      .getBookingForBillCollection(bookingId)
      .subscribe({
        next: (booking) => {
          this.selectedBooking = booking;
          // Set amount to due amount by default
          this.billCollectionForm.patchValue({
            amount: booking.dueAmount,
          });
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load booking details:', err);
          const errorMessage =
            err?.error?.message ||
            err?.message ||
            'Failed to load booking details';
          this.toastr.error(errorMessage);
          this.selectedBooking = null;
          this.isLoading = false;
        },
      });
  }

  validateAmount() {
    const amount = this.billCollectionForm.get('amount')?.value || 0;
    const dueAmount = this.selectedBooking?.dueAmount || 0;

    if (amount > dueAmount) {
      this.toastr.warning(
        `Amount cannot exceed due amount (${dueAmount.toFixed(2)})`
      );
      this.billCollectionForm.patchValue({ amount: dueAmount });
    }
  }

  onSubmit() {
    if (this.billCollectionForm.invalid) {
      this.billCollectionForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields');
      return;
    }

    const formValue = this.billCollectionForm.value;

    // Validate amount
    if (formValue.amount <= 0) {
      this.toastr.error('Amount must be greater than 0');
      return;
    }

    if (
      this.selectedBooking &&
      formValue.amount > this.selectedBooking.dueAmount
    ) {
      this.toastr.error(
        `Amount cannot exceed due amount (${this.selectedBooking.dueAmount.toFixed(
          2
        )})`
      );
      return;
    }

    this.isSubmitting = true;

    const payload: ITransactionRequest = {
      id: this.isEditing ? formValue.id : undefined,
      transactionCode: formValue.transactionCode,
      transactionType: TransactionType.BILL_COLLECTION,
      transactionFlow: TransactionFlow.IN,
      customerId: this.selectedBooking?.customerId,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      bookingId: formValue.bookingId,
      amount: formValue.amount,
      paymentMethod: formValue.paymentMethod,
      note: formValue.note,
      entityName: 'BOOKING',
      entityId: formValue.bookingId,
      description: `Bill Collection - ${
        this.selectedBooking?.bookingNumber || ''
      } - ${this.selectedBooking?.customerName || ''}`,
      discountAmount: 0,
      adjustmentValue: 0,
    };

    const request$ = this.isEditing
      ? this.transactionService.update(formValue.id, payload)
      : this.transactionService.create(payload);

    request$.subscribe({
      next: () => {
        this.router.navigate(['/bill-collection/list']);
      },
      error: () => {
        this.isSubmitting = false;
      },
    });
  }

  onSaveAndPrint() {
    if (this.billCollectionForm.invalid) {
      this.billCollectionForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields');
      return;
    }

    const formValue = this.billCollectionForm.value;

    // Validate amount
    if (formValue.amount <= 0) {
      this.toastr.error('Amount must be greater than 0');
      return;
    }

    if (
      this.selectedBooking &&
      formValue.amount > this.selectedBooking.dueAmount
    ) {
      this.toastr.error(
        `Amount cannot exceed due amount (${this.selectedBooking.dueAmount.toFixed(
          2
        )})`
      );
      return;
    }

    this.isSubmitting = true;

    const payload: ITransactionRequest = {
      id: this.isEditing ? formValue.id : undefined,
      transactionCode: formValue.transactionCode,
      transactionType: TransactionType.BILL_COLLECTION,
      transactionFlow: TransactionFlow.IN,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      bookingId: formValue.bookingId,
      amount: formValue.amount,
      paymentMethod: formValue.paymentMethod,
      note: formValue.note,
      entityName: 'BOOKING',
      entityId: formValue.bookingId,
      description: `Bill Collection - ${
        this.selectedBooking?.bookingNumber || ''
      } - ${this.selectedBooking?.customerName || ''}`,
      discountAmount: 0,
      adjustmentValue: 0,
    };

    const request$ = this.isEditing
      ? this.transactionService.update(formValue.id, payload)
      : this.transactionService.create(payload);

    request$.subscribe({
      next: (response) => {
        this.router.navigate([
          '/transaction/receipt-print',
          response.id,
          'bill-collection-list',
        ]);
      },
      error: () => {
        this.isSubmitting = false;
      },
    });
  }

  loadExistingTransaction(id: string) {
    this.isLoading = true;
    this.transactionService.getById(id).subscribe({
      next: (transaction) => {
        this.transactionCode = transaction.transactionCode;
        this.billCollectionForm.patchValue({
          id: transaction.id,
          transactionCode: transaction.transactionCode,
          transactionDate: new Date(transaction.transactionDate)
            .toISOString()
            .split('T')[0],
          bookingId: transaction.bookingId || '',
          branchId: transaction.branchId,
          amount: Math.abs(transaction.amount),
          paymentMethod: transaction.paymentMethod,
          note: transaction.note || '',
        });

        // Load booking details if bookingId exists
        if (transaction.bookingId) {
          this.onBookingChange(transaction.bookingId);
        }

        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.router.navigate(['/bill-collection/list']);
      },
    });
  }

  cancel() {
    this.router.navigate(['/bill-collection/list']);
  }

  reset() {
    this.selectedBooking = null;
    this.initForm();
    this.generateTransactionCode();
    this.loadBookingsWithDue();
  }
}
