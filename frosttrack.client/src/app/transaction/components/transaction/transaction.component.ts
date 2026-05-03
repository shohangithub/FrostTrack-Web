import { Component, OnInit, ViewChild } from '@angular/core';
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
import { NgSelectModule } from '@ng-select/ng-select';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ModalOption } from 'app/config/modal-option';
import { Subject } from 'rxjs';
import { ITransactionHeadLookup } from 'app/common/models/transaction-head.interface';
import { TransactionHeadService } from 'app/common/services/transaction-head.service';
import { TransactionReceiptPrintComponent } from '../transaction-receipt-print/transaction-receipt-print.component';
import { AddTransactionHeadComponent } from 'app/common/components/transaction-head/add-transaction-head/add-transaction-head.component';

@Component({
  selector: 'app-transaction',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NgSelectModule,
    TransactionReceiptPrintComponent,
  ],
  templateUrl: './transaction.component.html',
})
export class TransactionComponent implements OnInit {
  @ViewChild(TransactionReceiptPrintComponent)
  receiptComponent!: TransactionReceiptPrintComponent;
  transactionForm!: FormGroup;
  isLoading = false;
  isSubmitted = false;
  isEditing = false;
  isGeneratingCode = false;
  selectedBranch!: number;
  private generatedCode: string = '';
  private savedTransactionId: string = '';

  // Properties for inline printing
  receiptId: string = '';
  showReceipt: boolean = false;
  shouldAutoPrint: boolean = false;

  transactionHeads: ITransactionHeadLookup[] = [];
  transactionHeadLoading = false;
  private transactionHeadSubject: Subject<string> = new Subject<string>();
  private editableTransactionHeadId?: string;

  constructor(
    private fb: FormBuilder,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private layoutService: LayoutService,
    private transactionHeadService: TransactionHeadService,
    private modalService: NgbModal,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditing = true;
      // Fetch transaction heads first, then load transaction
      this.fetchTransactionHeads(() => {
        this.loadTransaction(id);
      });
    } else {
      this.fetchTransactionHeads();
      this.generateCode();
    }

    this.transactionHeadSubject.subscribe((value: string) => {
      const selectedHead = this.transactionHeads.find((x) => x.id === value);
      if (selectedHead) {
        this.transactionForm.patchValue({
          transactionHead: selectedHead,
          transactionFlow: selectedHead.type,
        });
      }
    });
  }

  initForm(): void {
    this.transactionForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      transactionCode: ['', [Validators.required]],
      transactionDate: [new Date().systemFormat(), [Validators.required]],
      transactionHead: [null, [Validators.required]],
      transactionFlow: [''],
      branchId: [this.selectedBranch, [Validators.required]],
      amount: [null, [Validators.required, Validators.min(0)]],
      note: [''],
    });

    // Watch for transactionHead changes to auto-populate transactionFlow
    this.transactionForm
      .get('transactionHead')
      ?.valueChanges.subscribe((value) => {
        if (value && value.type) {
          this.transactionForm.patchValue(
            {
              transactionFlow: value.type,
            },
            { emitEvent: false },
          );
        }
      });
  }

  fetchTransactionHeads(callback?: () => void): void {
    this.transactionHeadLoading = true;
    this.transactionHeadService.getTransactionLookup().subscribe({
      next: (data) => {
        this.transactionHeads = data;
        this.transactionHeadLoading = false;
        if (callback) {
          callback();
        }
      },
      error: () => {
        this.transactionHeadLoading = false;
      },
    });
  }

  addTransactionHead(): void {
    const modalRef = this.modalService.open(
      AddTransactionHeadComponent,
      ModalOption.lg,
    );
    modalRef.result.then(
      (result: string) => {
        if (result) {
          this.editableTransactionHeadId = result;
          this.fetchTransactionHeads();
          setTimeout(() => {
            this.transactionHeadSubject.next(result);
          }, 300);
        }
      },
      () => {},
    );
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

        // Find the transaction head by matching the transactionHeadId
        const transactionHead = this.transactionHeads.find(
          (th) => th.id === transaction.transactionHeadId,
        );

        this.transactionForm.patchValue({
          id: transaction.id,
          transactionCode: transaction.transactionCode,
          transactionDate: new Date(transaction.transactionDate).systemFormat(),
          transactionHead: transactionHead || null,
          transactionFlow: transactionHead?.type || '',
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
      id: formValue.id,
      transactionCode: formValue.transactionCode,
      transactionHeadId: formValue.transactionHead?.id,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      amount: formValue.amount,
      note: formValue.note,
      paymentMethod: 'CASH', // Default to CASH
      description: `${formValue.transactionHead?.name} - ${formValue.transactionFlow}`,
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
        if (!this.isEditing) {
          this.reset();
        } else {
          this.router.navigate(['/transaction/list']);
        }
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

    this.shouldAutoPrint = true;
    this.isSubmitted = true;

    const payload = {
      id: formValue.id,
      transactionCode: formValue.transactionCode,
      transactionHeadId: formValue.transactionHead?.id,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      amount: formValue.amount,
      note: formValue.note,
      paymentMethod: 'CASH', // Default to CASH
      description: `${formValue.transactionHead?.name} - ${formValue.transactionFlow}`,
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
        if (this.shouldAutoPrint) {
          this.loadReceiptForPrint();
        }
        if (!this.isEditing) {
          this.reset();
        }
      },
      error: () => {
        // BaseService already handles error toasts via ErrorHandlerService
        this.isSubmitted = false;
        this.shouldAutoPrint = false;
      },
    });
  }

  loadReceiptForPrint(): void {
    console.log(
      'Loading receipt for print, transaction ID:',
      this.savedTransactionId,
    );
    this.receiptId = this.savedTransactionId;
    this.showReceipt = true;

    // Wait for receipt component to load data before triggering print
    setTimeout(() => {
      console.log('Triggering receipt print');
      if (this.receiptComponent) {
        this.receiptComponent.triggerPrint();
      }
    }, 1500);
  }

  cancel(): void {
    this.router.navigate(['/transaction/list']);
  }

  reset(): void {
    this.transactionForm.reset({
      id: '00000000-0000-0000-0000-000000000000',
      transactionCode: '',
      transactionDate: new Date().systemFormat(),
      transactionHead: null,
      transactionFlow: '',
      branchId: this.selectedBranch,
      amount: null,
      note: '',
    });
    this.generateCode();
    this.isSubmitted = false;
  }
}
