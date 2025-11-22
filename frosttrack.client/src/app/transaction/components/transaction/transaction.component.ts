import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { TransactionService } from '../../services/transaction.service';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-transaction',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './transaction.component.html',
})
export class TransactionComponent implements OnInit {
  transactionForm!: FormGroup;
  isLoading = false;
  isSubmitted = false;
  isEditing = false;
  isGeneratingCode = false;
  selectedBranch!: number;
  private generatedCode: string = '';
  private savedTransactionId: string = '';

  // Transaction Types with Pascal Case labels
  transactionTypes = [
    { value: 'BILL_COLLECTION', label: 'Bill Collection' },
    { value: 'OFFICE_COST', label: 'Office Cost' },
    { value: 'BILL_PAYMENT', label: 'Bill Payment' },
    { value: 'ADJUSTMENT', label: 'Adjustment' },
    { value: 'REFUND', label: 'Refund' },
  ];

  // Transaction Flows
  transactionFlows = [
    { value: 'IN', label: 'In' },
    { value: 'OUT', label: 'Out' },
  ];

  constructor(
    private fb: FormBuilder,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditing = true;
      this.loadTransaction(id);
    } else {
      this.generateCode();
    }
  }

  initForm(): void {
    this.transactionForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      transactionCode: ['', [Validators.required]],
      transactionDate: [new Date().systemFormat(), [Validators.required]],
      transactionType: ['', [Validators.required]],
      transactionFlow: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      amount: [null, [Validators.required, Validators.min(0)]],
      note: [''],
    });
  }

  generateCode(): void {
    this.isGeneratingCode = true;
    this.transactionService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.transactionForm.patchValue({ transactionCode: response.code });
        this.isGeneratingCode = false;
      },
      error: () => {
        // BaseService already handles error toasts via ErrorHandlerService
        this.isGeneratingCode = false;
      },
    });
  }

  loadTransaction(id: string): void {
    this.isLoading = true;
    this.transactionService.getById(id).subscribe({
      next: (transaction) => {
        this.generatedCode = transaction.transactionCode;
        this.transactionForm.patchValue({
          id: transaction.id,
          transactionCode: transaction.transactionCode,
          transactionDate: new Date(transaction.transactionDate).systemFormat(),
          transactionType: transaction.transactionType,
          transactionFlow: transaction.transactionFlow,
          branchId: transaction.branchId,
          amount: Math.abs(transaction.amount), // Show as positive in form
          note: transaction.note || '',
        });
        this.isLoading = false;
      },
      error: () => {
        // BaseService already handles error toasts via ErrorHandlerService
        this.isLoading = false;
        this.router.navigate(['/transaction/list']);
      },
    });
  }

  onSubmit(): void {
    if (this.transactionForm.invalid) {
      this.transactionForm.markAllAsTouched();
      return;
    }

    const formValue = this.transactionForm.value;

    // Validate transaction code matches generated code
    if (formValue.transactionCode !== this.generatedCode) {
      this.toastr.error('Transaction code mismatch!');
      return;
    }

    this.isSubmitted = true;

    const payload = {
      ...formValue,
      paymentMethod: 'CASH', // Default to CASH
      entityName: 'GENERAL',
      entityId: '00000000-0000-0000-0000-000000000000',
      description: `${this.getTransactionTypeLabel(
        formValue.transactionType
      )} - ${this.getTransactionFlowLabel(formValue.transactionFlow)}`,
      discountAmount: 0,
      adjustmentValue: 0,
    };

    const request$ = this.isEditing
      ? this.transactionService.update(formValue.id, payload)
      : this.transactionService.create(payload);

    request$.subscribe({
      next: (response) => {
        // BaseService already handles success toasts via ErrorHandlerService
        this.savedTransactionId = response.id;
        this.router.navigate(['/transaction/list']);
      },
      error: () => {
        // BaseService already handles error toasts via ErrorHandlerService
        this.isSubmitted = false;
      },
    });
  }

  onSaveAndPrint(): void {
    if (this.transactionForm.invalid) {
      this.transactionForm.markAllAsTouched();
      return;
    }

    const formValue = this.transactionForm.value;

    // Validate transaction code matches generated code
    if (formValue.transactionCode !== this.generatedCode) {
      this.toastr.error('Transaction code mismatch!');
      return;
    }

    this.isSubmitted = true;

    const payload = {
      ...formValue,
      paymentMethod: 'CASH', // Default to CASH
      entityName: 'GENERAL',
      entityId: '00000000-0000-0000-0000-000000000000',
      description: `${this.getTransactionTypeLabel(
        formValue.transactionType
      )} - ${this.getTransactionFlowLabel(formValue.transactionFlow)}`,
      discountAmount: 0,
      adjustmentValue: 0,
    };

    const request$ = this.isEditing
      ? this.transactionService.update(formValue.id, payload)
      : this.transactionService.create(payload);

    request$.subscribe({
      next: (response) => {
        // BaseService already handles success toasts via ErrorHandlerService
        this.router.navigate([
          '/transaction/receipt-print',
          response.id,
          'list',
        ]);
      },
      error: () => {
        // BaseService already handles error toasts via ErrorHandlerService
        this.isSubmitted = false;
      },
    });
  }

  getTransactionTypeLabel(value: string): string {
    const type = this.transactionTypes.find((t) => t.value === value);
    return type ? type.label : value;
  }

  getTransactionFlowLabel(value: string): string {
    const flow = this.transactionFlows.find((f) => f.value === value);
    return flow ? flow.label : value;
  }

  cancel(): void {
    this.router.navigate(['/transaction/list']);
  }
}
