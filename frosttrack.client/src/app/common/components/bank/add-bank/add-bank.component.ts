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
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { BankService } from '../../../services/bank.service';
import { IBankRequest, IBankResponse } from '../../../models/bank.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-add-bank',
  templateUrl: './add-bank.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [BankService],
})
export class AddBankComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  private generatedCode: string = '';
  isGeneratingCode: boolean = false;

  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private bankService: BankService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      bankName: new UntypedFormControl(),
      bankCode: new UntypedFormControl(),
      bankBranch: new UntypedFormControl(),
      accountNumber: new UntypedFormControl(),
      accountTitle: new UntypedFormControl(),
      swiftCode: new UntypedFormControl(),
      routingNumber: new UntypedFormControl(),
      ibanNumber: new UntypedFormControl(),
      contactPerson: new UntypedFormControl(),
      contactPhone: new UntypedFormControl(),
      contactEmail: new UntypedFormControl(),
      address: new UntypedFormControl(),
      openingBalance: new UntypedFormControl(),
      currentBalance: new UntypedFormControl(),
      description: new UntypedFormControl(),
      isMainAccount: new UntypedFormControl(),
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
      bankName: [null, [Validators.required]],
      bankCode: [null, [Validators.required]],
      bankBranch: [null, [Validators.required]],
      accountNumber: [null, [Validators.required]],
      accountTitle: [null, [Validators.required]],
      swiftCode: [null],
      routingNumber: [null],
      ibanNumber: [null],
      contactPerson: [null],
      contactPhone: [null],
      contactEmail: [null, [Validators.email]],
      address: [null],
      openingBalance: [null],
      currentBalance: [null],
      description: [null],
      isMainAccount: [false],
      isActive: [true, [Validators.required]],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.bankService.getById(this.row.id).subscribe({
      next: (response: IBankResponse) => {
        this.editForm.setValue({
          id: response.id,
          bankName: response.bankName,
          bankCode: response.bankCode,
          bankBranch: response.bankBranch,
          accountNumber: response.accountNumber,
          accountTitle: response.accountTitle,
          swiftCode: response.swiftCode,
          routingNumber: response.routingNumber,
          ibanNumber: response.ibanNumber,
          contactPerson: response.contactPerson,
          contactPhone: response.contactPhone,
          contactEmail: response.contactEmail,
          address: response.address,
          openingBalance: response.openingBalance,
          currentBalance: response.currentBalance,
          description: response.description,
          isMainAccount: response.isMainAccount || false,
          isActive: response.isActive || false,
        });
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(formatErrorMessage(err));
        this.isLoading = false;
      },
    });
  }

  // add new record
  add(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      const payload: IBankRequest = { ...formData };
      if (payload.bankCode != this.generatedCode)
        this.toastr.error('Bank code mismatched !');

      payload.openingBalance = payload.openingBalance ?? 0;
      payload.currentBalance = payload.currentBalance ?? 0;

      this.bankService.create(payload).subscribe({
        next: (response: IBankResponse) => {
          this.isSubmitted = false;
          this.activeModal.close({ success: true, data: response });
        },
        error: () => {
          this.isSubmitted = false;
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }
  // edit a record
  edit(form: UntypedFormGroup) {
    if (this.editForm.valid) {
      this.isSubmitted = true;
      const formData = form.value;
      const payload: IBankRequest = { ...formData };
      payload.openingBalance = payload.openingBalance ?? 0;
      payload.currentBalance = payload.currentBalance ?? 0;
      this.bankService.update(formData.id, payload).subscribe({
        next: (response: IBankResponse) => {
          this.isSubmitted = false;
          this.activeModal.close({ success: true, data: response });
        },
        error: () => {
          this.isSubmitted = false;
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.bankService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('bankCode')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: (err: ErrorResponse) => {
        this.isGeneratingCode = false;
        const errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }
}
