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
  ICustomerPaymentRequest,
} from '../../services/bill-collection.service';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { BankService } from 'app/common/services/bank.service';
import { ILookup } from '@core/models/lookup';
import { dateInputFormat, todayInputFormat } from 'app/utils/date-utils';

@Component({
  selector: 'app-delivery-bill-collection',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    RouterLink,
  ],
  templateUrl: './delivery-bill-collection.component.html',
})
export class DeliveryBillCollectionComponent implements OnInit {
  billCollectionForm!: FormGroup;
  customers: ICustomerListResponse[] = [];
  customerBalance: ICustomerBalanceSummary | null = null;
  banks: ILookup<number>[] = [];

  customerLoading = false;
  balanceLoading = false;
  isLoading = false;
  isSubmitting = false;
  isGeneratingCode = false;
  isEditing = false;
  saveAndPrint = false;
  transactionCode = '';
  selectedBranch!: number;

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
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();
    this.loadCustomers();
    this.loadBanks();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditing = true;
      this.loadExistingTransaction(id);
    } else {
      this.generateTransactionCode();

      // Check if customerId is passed via query params (e.g. from Due Report)
      const qCustomerId = this.route.snapshot.queryParamMap.get('customerId');
      if (qCustomerId) {
        const cId = Number(qCustomerId);
        this.billCollectionForm.patchValue({ customerId: cId });
        this.loadCustomerBalance(cId);
      }
    }
  }

  loadBanks() {
    this.bankService.getLookup().subscribe({
      next: (res) => (this.banks = res),
      error: (err) => console.error('Failed to load banks:', err),
    });
  }

  initForm() {
    this.billCollectionForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      transactionCode: ['', Validators.required],
      transactionDate: [
        todayInputFormat(),
        Validators.required,
      ],
      customerId: [null, Validators.required],
      branchId: [this.selectedBranch, Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentMethod: ['CASH', Validators.required],
      bankId: [null],
      paymentReference: [''],
      note: [''],
    });

    this.billCollectionForm
      .get('customerId')
      ?.valueChanges.subscribe((customerId) => {
        if (customerId) {
          this.loadCustomerBalance(customerId);
        } else {
          this.customerBalance = null;
          this.billCollectionForm.patchValue({ amount: 0 });
        }
      });

    this.billCollectionForm
      .get('paymentMethod')
      ?.valueChanges.subscribe((pm) => {
        const bankControl = this.billCollectionForm.get('bankId');
        if (pm === 'BANK_TRANSFER' || pm === 'CHEQUE') {
          bankControl?.setValidators([Validators.required]);
        } else {
          bankControl?.clearValidators();
          bankControl?.setValue(null);
        }
        bankControl?.updateValueAndValidity();
      });
  }

  loadCustomers() {
    this.customerLoading = true;
    this.customerService.getList().subscribe({
      next: (response) => {
        this.customers = response;
        this.customerLoading = false;
      },
      error: (err) => {
        console.error('Failed to load customers:', err);
        this.toastr.error('Failed to load customers');
        this.customerLoading = false;
      },
    });
  }

  loadCustomerBalance(customerId: number) {
    this.balanceLoading = true;
    this.billCollectionService.getCustomerBalance(customerId).subscribe({
      next: (summary) => {
        this.customerBalance = summary;
        this.balanceLoading = false;
        if (!this.isEditing && summary.netDue > 0) {
          // Pre-fill amount with net due for quick collection
          this.billCollectionForm.patchValue({ amount: summary.netDue });
        }
      },
      error: (err) => {
        console.error('Failed to load customer balance:', err);
        this.balanceLoading = false;
      },
    });
  }

  payFullDue() {
    if (this.customerBalance && this.customerBalance.netDue > 0) {
      this.billCollectionForm.patchValue({ amount: this.customerBalance.netDue });
    }
  }

  get formAmount(): number {
    return Number(this.billCollectionForm?.get('amount')?.value) || 0;
  }

  get isSubmitDisabled(): boolean {
    if (this.isSubmitting) return true;
    if (this.billCollectionForm.invalid) return true;
    if (this.formAmount <= 0) return true;
    return false;
  }

  loadExistingTransaction(id: string) {
    this.isLoading = true;
    this.transactionService.getById(id).subscribe({
      next: (transaction) => {
        this.billCollectionForm.patchValue({
          id: transaction.id,
          transactionCode: transaction.transactionCode,
          transactionDate: dateInputFormat(new Date(transaction.transactionDate)),
          branchId: transaction.branchId,
          customerId: transaction.customerId,
          amount: transaction.amount,
          paymentMethod: transaction.paymentMethod,
          bankId: (transaction as any).bankId || null,
          paymentReference: transaction.paymentReference,
          note: transaction.note,
        });

        if (transaction.customerId) {
          this.loadCustomerBalance(transaction.customerId);
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load transaction:', err);
        this.toastr.error('Failed to load transaction');
        this.isLoading = false;
        this.router.navigate(['/bill-collection/list']);
      },
    });
  }

  generateTransactionCode() {
    this.isGeneratingCode = true;
    this.transactionService.generateCode().subscribe({
      next: (response: any) => {
        this.transactionCode = response.code;
        this.billCollectionForm.patchValue({
          transactionCode: this.transactionCode,
        });
        this.isGeneratingCode = false;
      },
      error: (err: any) => {
        console.error('Failed to generate transaction code:', err);
        this.toastr.error('Failed to generate transaction code');
        this.isGeneratingCode = false;
      },
    });
  }

  onSubmit(printAfterSave: boolean = false) {
    if (this.billCollectionForm.invalid) {
      this.billCollectionForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields');
      return;
    }

    const formValue = this.billCollectionForm.getRawValue();

    if (!formValue.customerId) {
      this.toastr.error('Please select a customer');
      return;
    }

    if (formValue.amount <= 0) {
      this.toastr.error('Payment amount must be greater than zero');
      return;
    }

    this.isSubmitting = true;
    this.saveAndPrint = printAfterSave;

    const payload: ICustomerPaymentRequest = {
      transactionCode: formValue.transactionCode,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      customerId: formValue.customerId,
      amount: Number(formValue.amount),
      paymentMethod: formValue.paymentMethod,
      bankId: formValue.bankId,
      paymentReference: formValue.paymentReference,
      note: formValue.note,
    };

    this.billCollectionService.createCustomerPayment(payload).subscribe({
      next: (response) => {
        this.toastr.success('Customer payment recorded successfully!');
        if (printAfterSave) {
          this.router.navigate([
            '/transaction/receipt-print',
            response.id,
            'bill-collection-list',
          ]);
        } else {
          this.router.navigate(['/bill-collection/list']);
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Failed to record customer payment:', err);
      },
    });
  }

  printReceipt(paymentId: string) {
    this.router.navigate([
      '/transaction/receipt-print',
      paymentId,
      'bill-collection-list',
    ]);
  }

  cancel() {
    this.router.navigate(['/bill-collection/list']);
  }

  reset() {
    this.customerBalance = null;
    this.initForm();
    this.generateTransactionCode();
  }
}
