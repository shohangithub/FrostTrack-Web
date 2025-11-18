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
import { Subject } from 'rxjs';
import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { ProductService } from '../../../services/product.service';
import {
  IProductRequest,
  IProductResponse,
} from '../../../models/product.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { ProductCategoryService } from '../../../../common/services/product-category.service';
import { UnitConversionService } from '../../../../common/services/unit-conversion.service';
import { ILookup } from '../../../../core/models/lookup';
import { AddProductCategoryComponent } from '../../../../common/components/product-category/add-product-category/add-product-category.component';
import { ModalOption } from '../../../../config/modal-option';
import { AddBaseUnitComponent } from '../../../../common/components/base-unit/add-base-unit/add-base-unit.component';
import { CodeResponse } from '../../../../core/models/code-response';
import { NgSelectModule } from '@ng-select/ng-select';

@Component({
  selector: 'app-add-product',
  templateUrl: './add-product.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
    FormShimmerComponent,
  ],
  providers: [ProductService],
})
export class AddProductComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  private generatedCode: string = '';
  isGeneratingCode: boolean = false;
  productCategories: ILookup<number>[] = [];
  productCatLoading = false;
  productUnits: ILookup<number>[] = [];
  productUnitLoading = false;

  private editableProductCatId?: number;
  private productCatSubject: Subject<number> = new Subject<number>();
  private editableProductUnitId?: number;
  private productUnitSubject: Subject<number> = new Subject<number>();
  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private modalService: NgbModal,
    private productService: ProductService,
    private productCatService: ProductCategoryService,
    private unitConversionService: UnitConversionService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      productName: new UntypedFormControl(),
      productCode: new UntypedFormControl(),
      customBarcode: new UntypedFormControl(),
      productCategory: new UntypedFormControl(),
      defaultUnit: new UntypedFormControl(),
      imageUrl: new UntypedFormControl(),
      bookingRate: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    this.fetchProductCatLookup();
    this.fetchProductUnitLookup();
    if (this.isEditing) {
      this.getExistingData();
    } else {
      this.generateCode();
    }

    this.productCatSubject.subscribe((value: number) => {
      this.editForm
        .get('productCategory')
        ?.setValue(this.productCategories.find((x) => x.value == value));
    });

    this.productUnitSubject.subscribe((value: number) => {
      this.editForm
        .get('defaultUnit')
        ?.setValue(this.productUnits.find((x) => x.value == value));
    });
  }

  initFormData() {
    this.register = this.fb.group({
      productCode: ['', [Validators.required]],
      productName: ['', [Validators.required]],
      customBarcode: [''],
      productCategory: [1, [Validators.required]],
      defaultUnit: [null, [Validators.required]],
      imageUrl: [''],
      bookingRate: [0],
      isActive: [true],
    });
  }

  fetchProductCatLookup() {
    this.productCatLoading = true;
    this.productCatService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.productCategories = response;
        if (this.editableProductCatId) {
          this.productCatSubject.next(this.editableProductCatId);
        }
        this.productCatLoading = false;
      },
      error: () => {
        this.productCatLoading = false;
      },
    });
  }
  fetchProductUnitLookup() {
    this.productUnitLoading = true;
    this.unitConversionService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.productUnits = response;
        if (this.editableProductUnitId) {
          this.productUnitSubject.next(this.editableProductUnitId);
        }
        this.productUnitLoading = false;
      },
      error: () => {
        this.productUnitLoading = false;
      },
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.productService.getById(this.row.id).subscribe({
      next: (response: IProductResponse) => {
        this.editForm.setValue({
          id: response.id,
          productName: response.productName,
          productCode: response.productCode,
          customBarcode: response.customBarcode,
          imageUrl: response.imageUrl,
          bookingRate: response.bookingRate,
          isActive: response.isActive,
          productCategory:
            this.productCategories.find(
              (x) => x.value == response.categoryId
            ) || null,
          defaultUnit:
            this.productUnits.find((x) => x.value == response.defaultUnitId) ||
            null,
        });
        this.editableProductCatId = response.categoryId;
        this.editableProductUnitId = response.defaultUnitId;

        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  addCategory() {
    const modalRef = this.modalService.open(
      AddProductCategoryComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
      if (response?.success) {
        const obj: ILookup<number> = {
          value: response.data.id,
          text: response.data.categoryName,
        };
        this.productCategories = this.productCategories.insertThenClone(obj);
      }
    });
  }

  addUnit() {
    const modalRef = this.modalService.open(
      AddBaseUnitComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
      if (response?.success) {
        const obj: ILookup<number> = {
          value: response.data.id,
          text: response.data.unitName,
        };
        this.productUnits = this.productUnits.insertThenClone(obj);
      }
    });
  }

  // add new record
  add(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      formData.categoryId = 1; //formData.productCategory?.value;
      formData.defaultUnitId = formData.defaultUnit?.value;
      const payload: IProductRequest = { ...formData };

      if (payload.productCode != this.generatedCode)
        this.toastr.error('Product code mismatched !');

      this.productService.create(payload).subscribe({
        next: (response: IProductResponse) => {
          this.isSubmitted = false;
          response.categoryName =
            this.productCategories.find((x) => x.value == response.categoryId)
              ?.text ?? null;
          response.unitName =
            this.productUnits.find((x) => x.value == response.defaultUnitId)
              ?.text ?? null;
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
      const formData = { ...form.value };
      formData.categoryId = formData.productCategory?.value;
      formData.defaultUnitId = formData.defaultUnit?.value;
      const payload: IProductRequest = { ...formData };

      this.productService.update(formData.id, payload).subscribe({
        next: (response: IProductResponse) => {
          this.isSubmitted = false;
          response.categoryName =
            this.productCategories.find((x) => x.value == response.categoryId)
              ?.text ?? null;
          response.unitName =
            this.productUnits.find((x) => x.value == response.defaultUnitId)
              ?.text ?? null;
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
    this.productService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('productCode')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
      },
    });
  }
}
