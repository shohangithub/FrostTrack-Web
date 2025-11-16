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
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { CustomerService } from '../../../services/customer.service';
import {
  ICustomerRequest,
  ICustomerResponse,
} from '../../../models/customer.interface';
import { MessageHub } from '../../../../config/message-hub';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-add-customer',
  templateUrl: './add-customer.component.html',
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
  providers: [CustomerService],
})
export class AddCustomerComponent implements OnInit {
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
    private customerService: CustomerService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      customerName: new UntypedFormControl(),
      customerCode: new UntypedFormControl(),
      // customerBarcode: new UntypedFormControl(),
      customerMobile: new UntypedFormControl(),
      customerEmail: new UntypedFormControl(),
      officePhone: new UntypedFormControl(),
      address: new UntypedFormControl(),
      //imageUrl: new UntypedFormControl(),
      creditLimit: new UntypedFormControl(),
      openingBalance: new UntypedFormControl(),
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
      customerName: [null, [Validators.required]],
      customerCode: [null, [Validators.required]],
      customerMobile: [null, [Validators.required]],
      customerEmail: [null, [Validators.email]],
      officePhone: [null],
      address: [null],
      imageUrl: [null],
      creditLimit: [null],
      openingBalance: [null],
      isActive: [true, [Validators.required]],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.customerService.getById(this.row.id).subscribe({
      next: (response: ICustomerResponse) => {
        this.editForm.setValue({
          id: response.id,
          customerName: response.customerName,
          customerCode: response.customerCode,
          customerMobile: response.customerMobile,
          customerEmail: response.customerEmail,
          officePhone: response.officePhone,
          address: response.address,
          creditLimit: response.creditLimit,
          openingBalance: response.openingBalance,
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
      // formData.categoryId = formData.productCategory?.value;
      // formData.defaultUnitId = formData.defaultUnit?.value;
      const payload: ICustomerRequest = { ...formData };
      if (payload.customerCode != this.generatedCode)
        this.toastr.error('Customer code mismatched !');

      payload.creditLimit = payload.creditLimit ?? 0;
      payload.openingBalance = payload.openingBalance ?? 0;

      this.customerService.create(payload).subscribe({
        next: (response: ICustomerResponse) => {
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
      const payload: ICustomerRequest = { ...formData };
      payload.creditLimit = payload.creditLimit ?? 0;
      payload.openingBalance = payload.openingBalance ?? 0;
      this.customerService.update(formData.id, payload).subscribe({
        next: (response: ICustomerResponse) => {
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
    this.customerService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('customerCode')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: (err: ErrorResponse) => {
        this.isGeneratingCode = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }
}
