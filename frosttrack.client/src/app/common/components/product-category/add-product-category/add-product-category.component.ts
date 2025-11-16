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

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { ProductCategoryService } from '../../../services/product-category.service';
import {
  IProductCategoryRequest,
  IProductCategoryResponse,
} from '../../../models/product-category.interface';
import { MessageHub } from '../../../../config/message-hub';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';

@Component({
  selector: 'app-add-product-category',
  templateUrl: './add-product-category.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [ProductCategoryService],
})
export class AddProductCategoryComponent implements OnInit {
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
    private toastr: ToastrService, // Restore private for direct usage
    private productCategoryService: ProductCategoryService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      categoryName: new UntypedFormControl(),
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
      categoryName: ['', [Validators.required]],
      description: [''],
      isActive: [true, [Validators.required]],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.productCategoryService.getById(this.row.id).subscribe({
      next: (response: IProductCategoryResponse) => {
        this.editForm.setValue({
          id: response.id,
          categoryName: response.categoryName,
          description: response.description,
          isActive: response.isActive || false,
        });
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        // BaseService already handles error toasts via ErrorHandlerService
      },
    });
  }

  // add new record
  add(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const payload: IProductCategoryRequest = { ...form.value };
      this.productCategoryService.create(payload).subscribe({
        next: (response: IProductCategoryResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
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
      const payload: IProductCategoryRequest = { ...formData };
      this.productCategoryService.update(formData.id, payload).subscribe({
        next: (response: IProductCategoryResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: () => {
          this.isSubmitted = false;
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }
}
