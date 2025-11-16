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
import { DamageService } from '../../../services/damage.service';
import { ProductService } from '../../../../administration/services/product.service';
import { UnitConversionService } from '../../../services/unit-conversion.service';
import {
  IDamageRequest,
  IDamageResponse,
} from '../../../models/damage.interface';
import { ILookup } from '../../../../core/models/lookup';
import { CodeResponse } from '../../../../core/models/code-response';
import { IProductListWithStockResponse } from '../../../../administration/models/product.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';

@Component({
  selector: 'app-add-damage',
  templateUrl: './add-damage.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [DamageService],
})
export class AddDamageComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  private generatedCode: string = '';
  isGeneratingCode: boolean = false;

  products: ILookup<number>[] = [];
  productLoading = false;
  units: ILookup<number>[] = [];
  unitLoading = false;
  selectedProduct: IProductListWithStockResponse | null = null;
  fullProductList: IProductListWithStockResponse[] = [];

  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService, // Restore private for direct usage
    private damageService: DamageService,
    private productService: ProductService,
    private unitConversionService: UnitConversionService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      damageNumber: new UntypedFormControl(),
      damageDate: new UntypedFormControl(),
      productId: new UntypedFormControl(),
      unitId: new UntypedFormControl(),
      quantity: new UntypedFormControl(),
      unitCost: new UntypedFormControl(),
      totalCost: new UntypedFormControl(),
      reason: new UntypedFormControl(),
      description: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    this.fetchProductLookup();
    this.fetchUnitLookup();
    this.generateCode();

    if (this.isEditing && this.row) {
      this.getExistingData();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      id: [0],
      damageNumber: ['', [Validators.required]],
      damageDate: [
        new Date().toISOString().split('T')[0],
        [Validators.required],
      ],
      productId: [null, [Validators.required]],
      unitId: [null, [Validators.required]],
      quantity: [null, [Validators.required, Validators.min(0.01)]],
      unitCost: [0, [Validators.required, Validators.min(0)]],
      totalCost: [0, [Validators.required, Validators.min(0)]],
      reason: [''],
      description: [''],
      isActive: ['true', [Validators.required]],
    });

    // Calculate total cost when quantity or unit cost changes
    this.register
      .get('quantity')
      ?.valueChanges.subscribe(() => this.calculateTotal());
    this.register
      .get('unitCost')
      ?.valueChanges.subscribe(() => this.calculateTotal());

    // Calculate total cost for edit form as well
    this.editForm
      .get('quantity')
      ?.valueChanges.subscribe(() => this.calculateEditTotal());
    this.editForm
      .get('unitCost')
      ?.valueChanges.subscribe(() => this.calculateEditTotal());
  }

  calculateTotal() {
    const quantity = this.register.get('quantity')?.value || 0;
    const unitCost = this.register.get('unitCost')?.value || 0;
    const totalCost = quantity * unitCost;
    this.register.get('totalCost')?.setValue(totalCost, { emitEvent: false });
  }

  calculateEditTotal() {
    const quantity = this.editForm.get('quantity')?.value || 0;
    const unitCost = this.editForm.get('unitCost')?.value || 0;
    const totalCost = quantity * unitCost;
    this.editForm.get('totalCost')?.setValue(totalCost, { emitEvent: false });
  }

  fetchProductLookup() {
    this.productLoading = true;
    this.productService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.products = response;
        this.productLoading = false;
        // Also fetch full product list with stock information
        this.fetchProductsWithStock();
      },
      error: () => {
        this.productLoading = false;
      },
    });
  }

  fetchProductsWithStock() {
    this.productService.getListWithStock().subscribe({
      next: (response: IProductListWithStockResponse[]) => {
        this.fullProductList = response;
      },
      error: () => {
        // Handle error silently
      },
    });
  }

  onProductChange() {
    const productId = this.register.get('productId')?.value;
    if (productId) {
      this.selectedProduct =
        this.fullProductList.find((p) => p.id === +productId) || null;
      if (this.selectedProduct && this.selectedProduct.stockUnit) {
        this.selectedProduct.unitName = this.selectedProduct.stockUnit.unitName;

        // Set unit to default stock unit
        this.register
          .get('unitId')
          ?.setValue(this.selectedProduct.stockUnit.id);

        // Calculate unit cost from product's latest purchase rate × unit conversion value
        this.calculateUnitCostFromProduct();
      }
    } else {
      this.selectedProduct = null;
      this.register.get('unitId')?.setValue(null);
      this.register.get('unitCost')?.setValue(0);
    }
  }

  calculateUnitCostFromProduct() {
    if (
      this.selectedProduct &&
      this.selectedProduct.lastPurchaseRate &&
      this.selectedProduct.stockUnit
    ) {
      // Unit cost = Purchase Rate × Unit Conversion Value
      const unitCost =
        this.selectedProduct.lastPurchaseRate *
        this.selectedProduct.stockUnit.conversionValue;
      this.register.get('unitCost')?.setValue(unitCost);

      // Recalculate total cost
      this.calculateTotal();
    }
  }

  onUnitChange() {
    const unitId = this.register.get('unitId')?.value;
    if (
      unitId &&
      this.selectedProduct &&
      this.selectedProduct.lastPurchaseRate
    ) {
      // Find the selected unit's conversion value
      const selectedUnit = this.units.find((u) => u.value === +unitId);
      if (selectedUnit) {
        // Get the unit conversion details to find conversion value
        this.unitConversionService.getById(+unitId).subscribe({
          next: (unitConversion) => {
            // Unit cost = Purchase Rate × Unit Conversion Value
            const unitCost =
              this.selectedProduct!.lastPurchaseRate! *
              unitConversion.conversionValue;
            this.register.get('unitCost')?.setValue(unitCost);

            // Recalculate total cost
            this.calculateTotal();
          },
          error: () => {
            // If unable to get conversion value, use base purchase rate
            this.register
              .get('unitCost')
              ?.setValue(this.selectedProduct!.lastPurchaseRate || 0);
            this.calculateTotal();
          },
        });
      }
    }
  }

  onEditProductChange() {
    const productId = this.editForm.get('productId')?.value;
    if (productId) {
      const selectedProduct =
        this.fullProductList.find((p) => p.id === +productId) || null;
      if (selectedProduct && selectedProduct.stockUnit) {
        // Set unit to default stock unit
        this.editForm.get('unitId')?.setValue(selectedProduct.stockUnit.id);

        // Calculate unit cost from product's latest purchase rate × unit conversion value
        if (selectedProduct.lastPurchaseRate) {
          const unitCost =
            selectedProduct.lastPurchaseRate *
            selectedProduct.stockUnit.conversionValue;
          this.editForm.get('unitCost')?.setValue(unitCost);

          // Recalculate total cost
          this.calculateEditTotal();
        }
      }
    } else {
      this.editForm.get('unitId')?.setValue(null);
      this.editForm.get('unitCost')?.setValue(0);
    }
  }

  onEditUnitChange() {
    const unitId = this.editForm.get('unitId')?.value;
    const productId = this.editForm.get('productId')?.value;

    if (unitId && productId) {
      // Find the product from the full product list
      const product = this.fullProductList.find((p) => p.id === +productId);
      if (product && product.lastPurchaseRate) {
        // Get the unit conversion details to find conversion value
        this.unitConversionService.getById(+unitId).subscribe({
          next: (unitConversion) => {
            // Unit cost = Purchase Rate × Unit Conversion Value
            const unitCost =
              product.lastPurchaseRate! * unitConversion.conversionValue;
            this.editForm.get('unitCost')?.setValue(unitCost);

            // Recalculate total cost
            this.calculateEditTotal();
          },
          error: () => {
            // If unable to get conversion value, use base purchase rate
            this.editForm
              .get('unitCost')
              ?.setValue(product.lastPurchaseRate || 0);
            this.calculateEditTotal();
          },
        });
      }
    }
  }

  fetchUnitLookup() {
    this.unitLoading = true;
    this.unitConversionService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.units = response;
        this.unitLoading = false;
      },
      error: () => {
        this.unitLoading = false;
      },
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.damageService.getById(this.row.id).subscribe({
      next: (response: IDamageResponse) => {
        this.editForm.setValue({
          id: response.id,
          damageNumber: response.damageNumber,
          damageDate: response.damageDate.split('T')[0],
          productId: response.productId,
          unitId: response.unitId,
          quantity: response.quantity,
          unitCost: response.unitCost,
          totalCost: response.totalCost,
          reason: response.reason || '',
          description: response.description || '',
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
      const formData = { ...form.value };
      const payload: IDamageRequest = { ...formData };

      if (payload.damageNumber != this.generatedCode) {
        this.toastr.error('Damage number mismatched!');
        this.isSubmitted = false;
        return;
      }

      this.damageService.create(payload).subscribe({
        next: (response: IDamageResponse) => {
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
      const payload: IDamageRequest = { ...formData };
      this.damageService.update(formData.id, payload).subscribe({
        next: (response: IDamageResponse) => {
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
    this.damageService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('damageNumber')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
      },
    });
  }
}
