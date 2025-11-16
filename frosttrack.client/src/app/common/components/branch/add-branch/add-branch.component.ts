import { Component, Input, OnInit } from '@angular/core';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  UntypedFormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

import {
  COMMON_STATUS_LIST,
  DEFAULT_CURRENCY_LIST,
} from 'app/common/data/settings-data';
import { BranchService } from '../../../services/branch.service';
import {
  IBranchRequest,
  IBranchResponse,
} from '../../../models/branch.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-add-branch',
  templateUrl: './add-branch.component.html',
  standalone: true,
  imports: [
    RouterLink,
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [BranchService],
})
export class AddBranchComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  currencyList = DEFAULT_CURRENCY_LIST;
  private generatedCode: string = '';
  isGeneratingCode: boolean = false;

  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private branchService: BranchService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      name: new UntypedFormControl(),
      branchCode: new UntypedFormControl(),
      businessCurrency: new UntypedFormControl(),
      currencySymbol: new UntypedFormControl(),
      phone: new UntypedFormControl(),
      address: new UntypedFormControl(),
      autoInvoicePrint: new UntypedFormControl(),
      invoiceHeader: new UntypedFormControl(),
      invoiceFooter: new UntypedFormControl(),
      isMainBranch: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    if (this.isEditing) {
      this.getExistingData();
    } else {
      this.generateCode();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      name: [null, [Validators.required]],
      branchCode: [null, [Validators.required]],
      businessCurrency: [null],
      currencySymbol: [null],
      phone: [null, [Validators.required]],
      address: [null, [Validators.required]],
      autoInvoicePrint: [false],
      invoiceHeader: [null],
      invoiceFooter: [null],
      isMainBranch: [false],
      isActive: [true, [Validators.required]],
    });
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.branchService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('branchCode')?.setValue(this.generatedCode);
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
        // BaseService already handles error toasts via ErrorHandlerService
      },
    });
  }

  getExistingData() {
    this.isLoading = true;

    this.branchService.getById(this.row.id).subscribe({
      next: (response: IBranchResponse) => {
        this.register.patchValue({
          name: response.name,
          branchCode: response.branchCode,
          businessCurrency: response.businessCurrency,
          currencySymbol: response.currencySymbol,
          phone: response.phone,
          address: response.address,
          autoInvoicePrint: response.autoInvoicePrint,
          invoiceHeader: response.invoiceHeader,
          invoiceFooter: response.invoiceFooter,
          isMainBranch: response.isMainBranch,
          isActive: response.isActive,
        });
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        // BaseService already handles error toasts via ErrorHandlerService
      },
    });
  }

  onCurrencyChange(event: any) {
    const selectedCurrency = event.target.value;
    const currency = this.currencyList.find((c) => c.id === selectedCurrency);
    if (currency) {
      this.register.get('currencySymbol')?.setValue(currency.symbol);
    }
  }

  onSubmit() {
    this.isSubmitted = true;
    if (this.register.invalid) {
      return;
    }

    const payload: IBranchRequest = {
      name: this.register.value.name,
      branchCode: this.register.value.branchCode,
      businessCurrency: this.register.value.businessCurrency,
      currencySymbol: this.register.value.currencySymbol,
      phone: this.register.value.phone,
      address: this.register.value.address,
      autoInvoicePrint: this.register.value.autoInvoicePrint,
      invoiceHeader: this.register.value.invoiceHeader,
      invoiceFooter: this.register.value.invoiceFooter,
      isMainBranch: this.register.value.isMainBranch,
      isActive: this.register.value.isActive,
    };

    if (this.isEditing) {
      this.branchService.update(this.row.id, payload).subscribe({
        next: (response: IBranchResponse) => {
          this.activeModal.close({ success: true, data: response });
        },
        error: () => {
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    } else {
      this.branchService.create(payload).subscribe({
        next: (response: IBranchResponse) => {
          this.activeModal.close({ success: true, data: response });
        },
        error: () => {
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }

  closeModal() {
    this.activeModal.close();
  }

  // convenience getter for easy access to form fields
  get f() {
    return this.register.controls;
  }
}
