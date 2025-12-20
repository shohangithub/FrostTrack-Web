import { Component, OnInit, ViewChild } from '@angular/core';
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
import { IBillCollectionRequest } from 'app/transaction/models/transaction.interface';
import { BillCollectionService } from '../../services/bill-collection.service';
import { IBookingWithDueResponse } from '../../models/bill-collection.interface';
import { BillCollectionReceiptPrintComponent } from '../bill-collection-receipt-print/bill-collection-receipt-print.component';

@Component({
  selector: 'app-bill-collection',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    RouterLink,
    BillCollectionReceiptPrintComponent,
  ],
  templateUrl: './bill-collection.component.html',
})
export class BillCollectionComponent implements OnInit {
  @ViewChild(BillCollectionReceiptPrintComponent) receiptComponent!: BillCollectionReceiptPrintComponent;
  billCollectionForm!: FormGroup;
  bookings: { value: string; text: string }[] = [];
  selectedBooking: IBookingWithDueResponse | null = null;
  bookingLoading = false;
  isLoading = false;
  isSubmitting = false;
  isEditing = false;
  isGeneratingCode = false;
  transactionCode = '';
  selectedBranch!: number;

  // Properties for inline printing
  receiptId: string = '';
  showReceipt: boolean = false;
  shouldAutoPrint: boolean = false;

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
    this.isGeneratingCode = true;
    this.transactionService.generateCode().subscribe({
      next: (response) => {
        this.transactionCode = response.code;
        this.billCollectionForm.patchValue({ transactionCode: response.code });
        this.isGeneratingCode = false;
      },
      error: () => {
        this.toastr.error('Failed to generate transaction code');
        this.isGeneratingCode = false;
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
          if (this.isEditing) {
            const existingAmount =
              this.billCollectionForm.get('amount')?.value || 0;
            this.selectedBooking = {
              ...booking,
              dueAmount: booking.dueAmount + existingAmount,
              paidAmount: booking.paidAmount - existingAmount,
            };
          } else {
            this.selectedBooking = booking;
          }
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

    const payload: IBillCollectionRequest = {
      id: this.isEditing ? formValue.id : undefined,
      transactionCode: formValue.transactionCode,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      bookingId: formValue.bookingId,
      amount: formValue.amount,
      paymentMethod: formValue.paymentMethod,
      note: formValue.note,
    };

    const request$ = this.isEditing
      ? this.billCollectionService.updateBillCollection(formValue.id, payload)
      : this.billCollectionService.createBillCollection(payload);

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

    this.shouldAutoPrint = true;
    this.isSubmitting = true;

    const payload: IBillCollectionRequest = {
      id: this.isEditing ? formValue.id : undefined,
      transactionCode: formValue.transactionCode,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      bookingId: formValue.bookingId,
      amount: formValue.amount,
      paymentMethod: formValue.paymentMethod,
      note: formValue.note,
    };

    const request$ = this.isEditing
      ? this.billCollectionService.updateBillCollection(formValue.id, payload)
      : this.billCollectionService.createBillCollection(payload);

    request$.subscribe({
      next: (response) => {
        this.savedTransactionId = response.id;
        if (this.shouldAutoPrint) {
          this.loadReceiptForPrint();
        }
      },
      error: () => {
        this.isSubmitting = false;
        this.shouldAutoPrint = false;
      },
    });
  }

  private savedTransactionId: string = '';

  loadReceiptForPrint(): void {
    console.log('Loading bill collection receipt for print, transaction ID:', this.savedTransactionId);
    this.receiptId = this.savedTransactionId;
    this.showReceipt = true;

    // Wait for receipt component to load data before triggering print
    setTimeout(() => {
      console.log('Triggering bill collection receipt print');
      if (this.receiptComponent) {
        this.receiptComponent.triggerPrint();
      }
    }, 1500);
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
