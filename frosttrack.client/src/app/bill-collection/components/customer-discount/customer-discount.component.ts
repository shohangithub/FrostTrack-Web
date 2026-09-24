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
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import {
  BillCollectionService,
  ICustomerBalanceSummary,
  ICustomerDiscountItem,
  ICustomerDiscountRequest,
} from '../../services/bill-collection.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-customer-discount',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    RouterLink,
  ],
  templateUrl: './customer-discount.component.html',
  styleUrls: ['./customer-discount.component.scss'],
})
export class CustomerDiscountComponent implements OnInit {
  discountForm!: FormGroup;
  customers: ICustomerListResponse[] = [];
  customerBalance: ICustomerBalanceSummary | null = null;
  discountHistory: ICustomerDiscountItem[] = [];

  customerLoading = false;
  balanceLoading = false;
  historyLoading = false;
  isSubmitting = false;
  isGeneratingCode = false;
  transactionCode = '';
  selectedBranch!: number;

  quickReasons: string[] = [
    'Special Management Waiver (ম্যানেজমেন্ট বিশেষ মওকুফ)',
    'Wastage / Quality Allowance (ক্ষতিপূরণ / মান সমন্বয়)',
    'Early Full Settlement Concession (এককালীন পরিশোধ ছাড়)',
    'Rounding-Off Adjustment (রাউন্ডিং ব্যালেন্স সমন্বয়)',
    'Contractual Volume Discount (চুক্তিবদ্ধ ভলিউম কমিশন/ডিসকাউন্ট)',
    'Customer Dispute Settlement (মীমাংসা সমন্বয়)',
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private customerService: CustomerService,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();
    this.loadCustomers();
    this.generateTransactionCode();

    // Check if customerId query param is provided (e.g. from Due List)
    const qCustomerId = this.route.snapshot.queryParamMap.get('customerId');
    if (qCustomerId) {
      const cId = Number(qCustomerId);
      if (!isNaN(cId) && cId > 0) {
        this.discountForm.patchValue({ customerId: cId });
        this.onCustomerChange(cId);
      }
    }
  }

  initForm(): void {
    this.discountForm = this.fb.group({
      transactionCode: ['', Validators.required],
      transactionDate: [todayInputFormat(), Validators.required],
      customerId: [null, Validators.required],
      branchId: [this.selectedBranch, Validators.required],
      discountAmount: [null, [Validators.required, Validators.min(0.01)]],
      discountReason: ['', [Validators.required, Validators.minLength(3)]],
      note: [''],
    });

    this.discountForm.get('customerId')?.valueChanges.subscribe((cid) => {
      this.onCustomerChange(cid);
    });
  }

  loadCustomers(): void {
    this.customerLoading = true;
    this.customerService.getList().subscribe({
      next: (res) => {
        this.customers = res;
        this.customerLoading = false;
      },
      error: (err) => {
        console.error('Failed to load customers:', err);
        this.toastr.error('Failed to load customers');
        this.customerLoading = false;
      },
    });
  }

  onCustomerChange(customerId: number | null): void {
    if (customerId) {
      this.loadCustomerBalance(customerId);
      this.loadDiscountHistory(customerId);
    } else {
      this.customerBalance = null;
      this.discountHistory = [];
      this.discountForm.patchValue({ discountAmount: null, discountReason: '', note: '' });
    }
  }

  loadCustomerBalance(customerId: number): void {
    this.balanceLoading = true;
    this.billCollectionService.getCustomerBalance(customerId).subscribe({
      next: (summary) => {
        this.customerBalance = summary;
        this.balanceLoading = false;
      },
      error: (err) => {
        console.error('Failed to load customer balance:', err);
        this.toastr.error('Failed to load customer balance');
        this.balanceLoading = false;
      },
    });
  }

  loadDiscountHistory(customerId: number): void {
    this.historyLoading = true;
    this.billCollectionService.getCustomerDiscountHistory(customerId).subscribe({
      next: (history) => {
        this.discountHistory = history;
        this.historyLoading = false;
      },
      error: (err) => {
        console.error('Failed to load discount history:', err);
        this.historyLoading = false;
      },
    });
  }

  generateTransactionCode(): void {
    this.isGeneratingCode = true;
    this.transactionService.generateCode().subscribe({
      next: (res: any) => {
        this.transactionCode = res.code;
        this.discountForm.patchValue({ transactionCode: this.transactionCode });
        this.isGeneratingCode = false;
      },
      error: (err: any) => {
        console.error('Failed to generate transaction code:', err);
        this.toastr.error('Failed to generate voucher code');
        this.isGeneratingCode = false;
      },
    });
  }

  selectQuickReason(reason: string): void {
    this.discountForm.patchValue({ discountReason: reason });
  }

  applyFullDueWaiver(): void {
    if (this.currentNetDue > 0) {
      this.discountForm.patchValue({ discountAmount: this.currentNetDue });
    }
  }

  get discountAmountValue(): number {
    return Number(this.discountForm.get('discountAmount')?.value) || 0;
  }

  get currentNetDue(): number {
    return this.customerBalance ? Number(this.customerBalance.netDue) : 0;
  }

  get isDiscountExceedsDue(): boolean {
    if (!this.customerBalance) return false;
    return this.discountAmountValue > this.currentNetDue;
  }

  get netDueAfterDiscount(): number {
    return Math.max(0, this.currentNetDue - this.discountAmountValue);
  }

  get isSubmitDisabled(): boolean {
    if (this.isSubmitting) return true;
    if (this.discountForm.invalid) return true;
    if (this.discountAmountValue <= 0) return true;
    if (this.isDiscountExceedsDue) return true;
    if (!this.customerBalance || this.currentNetDue <= 0) return true;
    return false;
  }

  onSubmit(): void {
    if (this.discountForm.invalid) {
      this.discountForm.markAllAsTouched();
      this.toastr.error('Please complete all required fields');
      return;
    }

    if (!this.customerBalance || this.currentNetDue <= 0) {
      this.toastr.warning('Customer has no outstanding due to discount');
      return;
    }

    if (this.isDiscountExceedsDue) {
      this.toastr.error(
        `Discount amount (৳ ${this.discountAmountValue.toFixed(2)}) cannot exceed outstanding due (৳ ${this.currentNetDue.toFixed(2)})`
      );
      return;
    }

    const raw = this.discountForm.getRawValue();
    const payload: ICustomerDiscountRequest = {
      transactionCode: raw.transactionCode,
      transactionDate: raw.transactionDate,
      branchId: this.selectedBranch || raw.branchId,
      customerId: raw.customerId,
      totalDue: this.currentNetDue,
      discountAmount: Number(raw.discountAmount),
      currentDue: this.netDueAfterDiscount,
      discountReason: raw.discountReason.trim(),
      note: raw.note ? raw.note.trim() : null,
      bookingId: null,
    };

    this.isSubmitting = true;
    this.billCollectionService.createCustomerDiscount(payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;

        // Reset inputs while keeping selected customer
        this.discountForm.patchValue({
          discountAmount: null,
          discountReason: '',
          note: '',
        });
        this.discountForm.get('discountAmount')?.markAsUntouched();
        this.discountForm.get('discountReason')?.markAsUntouched();

        // Refresh balance and history
        this.loadCustomerBalance(payload.customerId);
        this.loadDiscountHistory(payload.customerId);

        // Generate new voucher code for next discount
        this.generateTransactionCode();
      },
      error: (err) => {
        console.error('Failed to create customer discount:', err);
        const errorMsg =
          err?.error?.message ||
          err?.error?.title ||
          'Failed to record customer discount';
        this.toastr.error(errorMsg);
        this.isSubmitting = false;
      },
    });
  }

  reset(): void {
    this.discountForm.patchValue({
      discountAmount: null,
      discountReason: '',
      note: '',
    });
    this.discountForm.markAsPristine();
    this.discountForm.markAsUntouched();
  }

  cancel(): void {
    this.router.navigate(['/bill-collection/list']);
  }
}
