import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import {
  IBankTransactionListResponse,
  IBankTransactionRequest,
} from '../../models/bank-transaction.interface';
import { BankService } from '../../services/bank.service';
import { CommonModule } from '@angular/common';
import { SwalConfirm } from 'app/theme-config';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  PaginationResult,
  PagingResponse,
} from '../../../core/models/pagination-result';
import { PaginationQuery } from '../../../core/models/pagination-query';
import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { DefaultPagination } from '../../../config/pagination';
import { LayoutService } from '@core/service/layout.service';
import { ILookup } from '../../../core/models/lookup';
import { CodeResponse } from '../../../core/models/code-response';
import { NgSelectModule } from '@ng-select/ng-select';
import { BankTransactionService } from 'app/common/services/bank-transaction.service';
import { AddBankComponent } from '../bank/add-bank/add-bank.component';
import { ModalOption } from '../../../config/modal-option';

@Component({
  selector: 'app-bank-transaction',
  templateUrl: './bank-transaction.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
  ],
  providers: [BankTransactionService, BankService],
  styles: [
    `
      .input-group-select {
        display: flex;
      }
      .input-group-select ng-select {
        flex: 1;
      }
      .input-group-select .input-group-append {
        display: flex;
      }
      .input-group-select .input-group-append .btn {
        border-top-left-radius: 0;
        border-bottom-left-radius: 0;
        border-left: none;
      }
      .input-group-select ng-select .ng-select-container {
        border-top-right-radius: 0;
        border-bottom-right-radius: 0;
      }
    `,
  ],
})
export class BankTransactionComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  // Component state
  activeTab: 'deposit' | 'withdraw' = 'deposit';
  data: IBankTransactionListResponse[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  reorderable = true;
  selected: IBankTransactionListResponse[] = [];
  scrollBarHorizontal = window.innerWidth < 1200;

  // Forms
  depositForm!: UntypedFormGroup;
  withdrawForm!: UntypedFormGroup;

  // Pagination
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;

  // Lookup data
  banks: ILookup<number>[] = [];
  statusList = COMMON_STATUS_LIST;
  selectedBankBalance: number = 0;
  selectedBankName: string = '';
  isLoadingBalance: boolean = false;

  // Form state
  isSubmitted = false;
  isGeneratingCode = false;
  private generatedCode: string = '';

  // Custom validator for maximum withdrawal amount
  maxAmountValidator = (control: any) => {
    if (this.activeTab === 'withdraw' && this.selectedBankBalance > 0) {
      const amount = control.value;
      if (amount && amount > this.selectedBankBalance) {
        return {
          maxAmount: {
            actual: amount,
            max: this.selectedBankBalance,
            message: `Amount cannot exceed available balance of $${this.selectedBankBalance.toFixed(
              2
            )}`,
          },
        };
      }
    }
    return null;
  };

  get isWithdrawTab(): boolean {
    return this.activeTab === 'withdraw';
  }

  // Data table
  selection!: SelectionType;

  MessageHub = {
    ADD: 'Transaction added successfully',
    UPDATE: 'Transaction updated successfully',
    DELETE_CONFIRM: 'Are you sure?',
    DELETE: 'Transaction deleted successfully',
  };

  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private bankTransactionService: BankTransactionService,
    private bankService: BankService,
    private layoutService: LayoutService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.selection = SelectionType.checkbox;
    this.layoutService.loadCurrentRoute();

    this.initializeForms();
  }

  ngOnInit() {
    this.fetchData();
    this.loadBanks();
    this.generateCode();

    //subject call change open text search
    this.searchSubject
      .pipe(debounceTime(1000), distinctUntilChanged())
      .subscribe((value: any) => {
        this.pagination.openText = value;
        this.fetchData();
      });
  }

  initializeForms() {
    this.depositForm = this.fb.group({
      id: [0],
      transactionNumber: ['', [Validators.required]],
      transactionDate: [
        new Date().toISOString().split('T')[0],
        [Validators.required],
      ],
      bankId: [null, [Validators.required]],
      transactionType: ['Deposit'],
      amount: [null, [Validators.required, Validators.min(0.01)]],
      reference: [''],
      description: [''],
      receiptNumber: [''],
      isActive: [true, [Validators.required]],
    });

    this.withdrawForm = this.fb.group({
      id: [0],
      transactionNumber: ['', [Validators.required]],
      transactionDate: [
        new Date().toISOString().split('T')[0],
        [Validators.required],
      ],
      bankId: [null, [Validators.required]],
      transactionType: ['Withdraw'],
      amount: [
        null,
        [Validators.required, Validators.min(0.01), this.maxAmountValidator],
      ],
      reference: [''],
      description: [''],
      receiptNumber: [''],
      isActive: [true, [Validators.required]],
    });

    // Subscribe to bank selection changes for both forms
    this.depositForm.get('bankId')?.valueChanges.subscribe((bankId) => {
      this.onBankSelectionChange(bankId);
    });

    this.withdrawForm.get('bankId')?.valueChanges.subscribe((bankId) => {
      this.onBankSelectionChange(bankId);
    });
  }

  get currentForm(): UntypedFormGroup {
    return this.activeTab === 'deposit' ? this.depositForm : this.withdrawForm;
  }

  switchTab(tab: 'deposit' | 'withdraw') {
    this.activeTab = tab;
    this.generateCode();

    // Load balance for the currently selected bank in the active form
    const currentBankId = this.currentForm.get('bankId')?.value;
    if (currentBankId) {
      this.loadBankBalance(currentBankId);
    }

    // Update validators when switching tabs
    this.updateAmountValidators();
  }

  async loadBanks() {
    try {
      const response = await this.bankService.getLookup().toPromise();
      this.banks = response || [];
    } catch (error) {
      console.error('Error loading banks:', error);
    }
  }

  onBankSelectionChange(bankId: number | null) {
    if (bankId) {
      this.loadBankBalance(bankId);
    } else {
      this.selectedBankBalance = 0;
      this.selectedBankName = '';
      // Reset amount field validators when bank is cleared
      this.updateAmountValidators();
    }
  }

  updateAmountValidators() {
    // Update validators for withdraw form amount field
    const withdrawAmountControl = this.withdrawForm.get('amount');
    if (withdrawAmountControl) {
      withdrawAmountControl.setValidators([
        Validators.required,
        Validators.min(0.01),
        this.maxAmountValidator,
      ]);
      withdrawAmountControl.updateValueAndValidity();
    }
  }

  loadBankBalance(bankId: number) {
    debugger;
    this.isLoadingBalance = true;
    const selectedBank = this.banks.find((bank) => bank.value === bankId);
    this.selectedBankName = selectedBank ? selectedBank.text : '';

    this.bankService.getCurrentBalance(bankId).subscribe({
      next: (balance: number) => {
        this.selectedBankBalance = balance;
        this.isLoadingBalance = false;
        // Update validators after balance is loaded
        this.updateAmountValidators();
      },
      error: (error) => {
        console.error('Error loading bank balance:', error);
        this.selectedBankBalance = 0;
        this.isLoadingBalance = false;
        // Update validators even on error
        this.updateAmountValidators();
      },
    });
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.bankTransactionService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.currentForm.get('transactionNumber')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
      },
    });
  }

  fetchData() {
    this.bankTransactionService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IBankTransactionListResponse>) => {
        this.data = response.data;
        this.paging = response.paging;
        this.loadingIndicator = false;
      },
      error: () => {
        this.loadingIndicator = false;
      },
    });
  }

  changePagination(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset;
    this.fetchData();
  }

  onSortring(event: any) {
    const sort = event.sorts[0];
    this.pagination.orderBy = sort.prop;
    this.pagination.isAscending = sort.dir === 'desc' ? false : true;
    this.fetchData();
  }

  // select record using check box
  onSelect({ selected }: { selected: any }) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);
    this.isRowSelected = this.selected.length > 0;
  }

  submitTransaction() {
    const form = this.currentForm;
    if (form.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      const payload: IBankTransactionRequest = { ...formData };

      if (payload.transactionNumber != this.generatedCode) {
        this.toastr.error('Transaction number mismatched!');
        this.isSubmitted = false;
        return;
      }

      // Convert string date to Date object
      payload.transactionDate = new Date(payload.transactionDate);

      this.bankTransactionService.create(payload).subscribe({
        next: () => {
          this.isSubmitted = false;
          const bankId = payload.bankId;
          this.resetForm();
          this.fetchData();
          this.generateCode();

          // Refresh bank balance after successful transaction
          if (bankId) {
            this.loadBankBalance(bankId);
          }
        },
        error: () => {
          this.isSubmitted = false;
        },
      });
    }
  }

  resetForm() {
    this.currentForm.reset();
    this.selectedBankBalance = 0;
    this.selectedBankName = '';
    this.initializeForms();
  }

  deleteSelected() {
    Swal.fire({
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        const ids = this.selected.map((x) => x.id);
        this.bankTransactionService.batchDelete(ids).subscribe({
          next: () => {
            this.selected.forEach((row) => {
              this.removeRecord(row);
            });
            this.selected = [];
            this.isRowSelected = false;
          },
          error: () => {},
        });
      }
    });
  }

  delete(row: any) {
    Swal.fire({
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.bankTransactionService.remove(row.id).subscribe({
          next: () => {
            this.removeRecord(row);
          },
          error: () => {
            // Error is handled by service
          },
        });
      }
    });
  }

  private removeRecord(row: any) {
    this.data = this.arrayRemove(this.data, row.id);
  }

  private arrayRemove(array: any[], id: any) {
    return array.filter(function (element) {
      return element.id !== id;
    });
  }

  searchSubject = new Subject<any>();
  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }

  addBank() {
    const modalRef = this.modalService.open(AddBankComponent, ModalOption.lg);
    modalRef.componentInstance.isEditing = false;
    modalRef.result.then(
      (result) => {
        if (result.success) {
          // Refresh banks list

          this.banks.push({
            value: result.data.id,
            text: result.data.bankName,
          });

          // Select the newly added bank
          this.currentForm.get('bankId')?.setValue(result.data.id);
        }
      },
      () => {
        // Modal dismissed
      }
    );
  }
}
