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
import { BaseUnitService } from '../../../services/base-unit.service';
import {
  IBaseUnitRequest,
  IBaseUnitResponse,
} from '../../../models/base-unit.interface';
import { MessageHub } from '../../../../config/message-hub';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';

@Component({
  selector: 'app-add-base-unit',
  templateUrl: './add-base-unit.component.html',
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
  providers: [BaseUnitService],
})
export class AddBaseUnitComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;

  constructor(
    private fb: UntypedFormBuilder,
    public modal: NgbActiveModal,
    private toastr: ToastrService,
    private productCategoryService: BaseUnitService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      unitName: new UntypedFormControl(),
      description: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
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
      unitName: ['', [Validators.required]],
      description: [''],
      isActive: [true, [Validators.required]],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.productCategoryService.getById(this.row.id).subscribe({
      next: (response: IBaseUnitResponse) => {
        this.editForm.setValue({
          id: response.id,
          unitName: response.unitName,
          description: response.description,
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
      const payload: IBaseUnitRequest = { ...form.value };
      this.productCategoryService.create(payload).subscribe({
        next: (response: IBaseUnitResponse) => {
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
      const payload: IBaseUnitRequest = { ...formData };
      this.productCategoryService.update(formData.id, payload).subscribe({
        next: (response: IBaseUnitResponse) => {
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
