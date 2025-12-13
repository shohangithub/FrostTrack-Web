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
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { TransactionHeadService } from '../../../services/transaction-head.service';
import {
  ITransactionHeadRequest,
  ITransactionHeadResponse,
} from '../../../models/transaction-head.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';

@Component({
  selector: 'app-add-transaction-head',
  templateUrl: './add-transaction-head.component.html',
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
  providers: [TransactionHeadService],
})
export class AddTransactionHeadComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;

  typeList = [
    { id: 'DEBIT', value: 'DEBIT' },
    { id: 'CREDIT', value: 'CREDIT' },
  ];

  displayTypeList = [
    { id: 'OUT', value: 'OUT', type: 'DEBIT' },
    { id: 'EXPENSE', value: 'EXPENSE', type: 'DEBIT' },
    { id: 'IN', value: 'IN', type: 'CREDIT' },
    { id: 'INCOME', value: 'INCOME', type: 'CREDIT' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    public modal: NgbActiveModal,
    private toastr: ToastrService,
    private transactionHeadService: TransactionHeadService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      code: new UntypedFormControl(),
      name: new UntypedFormControl(),
      type: new UntypedFormControl(),
      displayType: new UntypedFormControl(),
      sortOrder: new UntypedFormControl(),
      description: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
      colorCode: new UntypedFormControl(),
      iconClass: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    if (this.isEditing) {
      this.getExistingData();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      code: ['', [Validators.required]],
      name: ['', [Validators.required]],
      type: ['DEBIT', [Validators.required]],
      displayType: [''],
      sortOrder: [0],
      description: [''],
      isActive: [true, [Validators.required]],
      colorCode: [''],
      iconClass: [''],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.transactionHeadService.getById(this.row.id).subscribe({
      next: (response: ITransactionHeadResponse) => {
        this.editForm.setValue({
          id: response.id,
          code: response.code,
          name: response.name,
          type: response.type,
          displayType: response.displayType,
          sortOrder: response.sortOrder,
          description: response.description || '',
          isActive: response.isActive || false,
          colorCode: response.colorCode || '',
          iconClass: response.iconClass || '',
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
      const payload: ITransactionHeadRequest = { ...form.value };
      this.transactionHeadService.create(payload).subscribe({
        next: (response: ITransactionHeadResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: (err: ErrorResponse) => {
          this.isSubmitted = false;
          const errString = formatErrorMessage(err);
          this.toastr.error(errString);
        },
      });
    }
  }
  // edit a record
  edit(form: UntypedFormGroup) {
    if (this.editForm.valid) {
      this.isSubmitted = true;
      const formData = form.value;
      const payload: ITransactionHeadRequest = {
        code: formData.code,
        name: formData.name,
        type: formData.type,
        displayType: formData.displayType,
        sortOrder: formData.sortOrder,
        description: formData.description,
        isActive: formData.isActive,
        colorCode: formData.colorCode,
        iconClass: formData.iconClass,
      };
      this.transactionHeadService.update(formData.id, payload).subscribe({
        next: (response: ITransactionHeadResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: (err: ErrorResponse) => {
          this.isSubmitted = false;
          this.toastr.error(formatErrorMessage(err));
        },
      });
    }
  }
}
