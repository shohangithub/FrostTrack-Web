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
import { ScremerShimmerModule } from 'scremer-shimmer';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { SupplierService } from '../../../services/supplier.service';
import {
  ISupplierRequest,
  ISupplierResponse,
} from '../../../models/supplier.interface';
import { MessageHub } from '../../../../config/message-hub';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-add-supplier',
  templateUrl: './add-supplier.component.html',
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
  providers: [SupplierService],
})
export class AddSupplierComponent implements OnInit {
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
    private modalService: NgbModal,
    private supplierService: SupplierService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      supplierName: new UntypedFormControl(),
      supplierCode: new UntypedFormControl(),
      // supplierBarcode: new UntypedFormControl(),
      supplierMobile: new UntypedFormControl(),
      supplierEmail: new UntypedFormControl(),
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
      supplierName: [null, [Validators.required]],
      supplierCode: [null, [Validators.required]],
      supplierMobile: [null, [Validators.required]],
      supplierEmail: [null, [Validators.email]],
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
    this.supplierService.getById(this.row.id).subscribe({
      next: (response: ISupplierResponse) => {
        this.editForm.setValue({
          id: response.id,
          supplierName: response.supplierName,
          supplierCode: response.supplierCode,
          supplierMobile: response.supplierMobile,
          supplierEmail: response.supplierEmail,
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
      const payload: ISupplierRequest = { ...formData };
      if (payload.supplierCode != this.generatedCode)
        this.toastr.error('Supplier code mismatched !');

      payload.creditLimit = payload.creditLimit ?? 0;
      payload.openingBalance = payload.openingBalance ?? 0;

      this.supplierService.create(payload).subscribe({
        next: (response: ISupplierResponse) => {
          this.isSubmitted = false;
          this.activeModal.close({ success: true, data: response });
        },
        error: (err: ErrorResponse) => {
          this.isSubmitted = false;
          var errString = formatErrorMessage(err);
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
      const payload: ISupplierRequest = { ...formData };
      payload.creditLimit = payload.creditLimit ?? 0;
      payload.openingBalance = payload.openingBalance ?? 0;
      this.supplierService.update(formData.id, payload).subscribe({
        next: (response: ISupplierResponse) => {
          this.isSubmitted = false;
          this.activeModal.close({ success: true, data: response });
        },
        error: (err: ErrorResponse) => {
          this.isSubmitted = false;
          this.toastr.error(formatErrorMessage(err));
        },
      });
    }
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.supplierService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('supplierCode')?.setValue(response.code);
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
