import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  FormArray,
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { formatErrorMessage } from 'app/utils/server-error-handler';
import { Subject } from 'rxjs';
import { ProductDeliveryService } from 'app/product-delivery/services/product-delivery.service';
import {
  IProductDeliveryListResponse,
  IProductDeliveryRequest,
  IProductDeliveryResponse,
} from 'app/product-delivery/models/product-delivery.interface';
import { CodeResponse } from '@core/models/code-response';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { NgSelectModule } from '@ng-select/ng-select';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { ModalOption } from '../../../config/modal-option';
import { AddCustomerComponent } from 'app/common/components/customer/add-customer/add-customer.component';
import { UnitConversionService } from 'app/common/services/unit-conversion.service';

@Component({
  selector: 'app-product-delivery',
  templateUrl: './product-delivery.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
  ],
  providers: [ProductDeliveryService, AuthService],
})
export class ProductDeliveryComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IProductDeliveryListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IProductDeliveryListResponse[] = [];
  branchs: ILookup<number>[] = [];
  selectedBranch!: number;

  register!: UntypedFormGroup;
  productForm!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  private generatedCode: string = '';
  isGeneratingCode = false;
  isMainBranch: boolean = false;
  private branchSubject: Subject<number> = new Subject<number>();

  customerStockProducts: any[] = [];
  productLoading = false;
  customers: ICustomerListResponse[] = [];
  customerLoading = false;
  productUnits: ILookup<number>[] = [];
  productUnitLoading = false;

  private editableProductId?: number;
  private productSubject: Subject<number> = new Subject<number>();

  private editableCustomerId?: number;
  private customerSubject: Subject<number> = new Subject<number>();

  private editableProductUnitId?: number;
  private productUnitSubject: Subject<number> = new Subject<number>();

  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private productDeliveryService: ProductDeliveryService,
    private branchService: BranchService,
    private customerService: CustomerService,
    private route: ActivatedRoute,
    private authService: AuthService,
    private layoutService: LayoutService,
    private unitConversionService: UnitConversionService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.getUserBranch();
    this.initFormData();
    this.fetchBranchLookup();
    this.fetchCustomerLookup();
    this.fetchProductUnitLookup();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.getExistingData(id);

    this.branchSubject.subscribe((value: number) => {
      if (value === 1) {
        this.isMainBranch = true;
        this.register.get('branchId')?.setValue(value);
      }
    });

    this.productSubject.subscribe((value: number) => {
      this.productForm
        .get('product')
        ?.setValue(
          this.customerStockProducts.find((x) => x.productId === value)
        );
    });

    this.customerSubject.subscribe((value: number) => {
      this.register
        .get('customer')
        ?.setValue(this.customers.find((x) => x.id === value));
    });

    this.productUnitSubject.subscribe((value: number) => {
      this.productForm
        .get('deliveryUnit')
        ?.setValue(this.productUnits.find((x) => x.value == value));
    });
  }

  getUserBranch() {
    this.selectedBranch = this.authService.currentBranchId;
  }

  initFormData() {
    this.initProductForm();
    this.register = this.fb.group({
      id: [0],
      deliveryNumber: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      deliveryDate: [new Date().systemFormat(), [Validators.required]],
      customer: [null, [Validators.required]],
      notes: [''],
      productDeliveryDetails: this.fb.array([]),
    });
    this.generateCode();
  }

  initProductForm() {
    this.productForm = this.fb.group({
      id: [0],
      productDeliveryId: [0],
      product: [null, [Validators.required]],
      deliveryRate: [null, [Validators.required]],
      deliveryUnit: [null, [Validators.required]],
      deliveryQuantity: [null, [Validators.required]],
      baseQuantity: [0],
      baseRate: [0],
      availableStock: [{ value: 0, disabled: true }],
    });
  }

  getExistingData(id: any) {
    this.isLoading = true;
    this.productDeliveryService.getById(id).subscribe({
      next: (response: IProductDeliveryResponse) => {
        this.register.setValue({
          id: response.id,
          deliveryNumber: response.deliveryNumber,
          branchId: response.branchId,
          deliveryDate: response.deliveryDate,
          notes: response.notes || '',
          productDeliveryDetails: [],
          customer:
            this.customers.find((x) => x.id === response.customerId) || null,
        });

        for (const detail of response.productDeliveryDetails) {
          const item = this.fb.group({
            id: [detail.id],
            productId: [detail.productId, [Validators.required]],
            productName: [detail.product?.productName, [Validators.required]],
            productDeliveryId: [detail.productDeliveryId],
            deliveryUnitId: [detail.deliveryUnitId, [Validators.required]],
            deliveryUnitName: [
              detail.deliveryUnit?.unitName || '',
              [Validators.required],
            ],
            deliveryRate: [detail.deliveryRate, [Validators.required]],
            deliveryQuantity: [detail.deliveryQuantity, [Validators.required]],
            baseQuantity: [detail.baseQuantity],
            baseRate: [detail.baseRate],
          });
          this.productDeliveryDetails.push(item);
        }

        this.editableCustomerId = response.customerId;
        this.generatedCode = response.deliveryNumber;

        // Load customer stock after setting customer
        if (response.customerId) {
          this.loadCustomerStock(response.customerId);
        }

        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(formatErrorMessage(err));
        this.isLoading = false;
      },
    });
  }

  fetchBranchLookup() {
    this.branchService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.branchs = response;
        this.branchSubject.next(this.selectedBranch);
        this.loadingIndicator = false;
      },
      error: () => {
        this.loadingIndicator = false;
      },
    });
  }

  fetchCustomerLookup() {
    this.customerLoading = true;
    this.customerService.getList().subscribe({
      next: (response: ICustomerListResponse[]) => {
        this.customers = response;
        if (this.editableCustomerId) {
          this.customerSubject.next(this.editableCustomerId);
        }
        this.customerLoading = false;
      },
      error: () => {
        this.customerLoading = false;
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

  onCustomerChange() {
    const customer = this.register.get('customer')?.value;
    if (customer && customer.id) {
      this.loadCustomerStock(customer.id);
      // Clear product selection when customer changes
      this.productForm.reset();
      this.initProductForm();
    } else {
      this.customerStockProducts = [];
    }
  }

  loadCustomerStock(customerId: number) {
    this.productLoading = true;
    this.productDeliveryService
      .getCustomerStockByCustomerId(customerId)
      .subscribe({
        next: (response: any[]) => {
          this.customerStockProducts = response;
          this.productLoading = false;
        },
        error: () => {
          this.productLoading = false;
          this.toastr.error('Failed to load customer stock');
        },
      });
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.productDeliveryService.generateDeliveryNumber().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('deliveryNumber')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
      },
    });
  }

  setProductDetails() {
    const childForm = this.productForm;
    const product = childForm?.get('product')?.value;
    if (product) {
      const cardData: Array<any> = this.productDeliveryDetails.value;
      const existingProduct = cardData.find(
        (x) => x.productId === product.productId
      );

      if (existingProduct) {
        childForm
          ?.get('deliveryRate')
          ?.setValue(existingProduct.deliveryRate || 0);
        childForm
          ?.get('deliveryQuantity')
          ?.setValue(existingProduct.deliveryQuantity);
        childForm
          ?.get('deliveryUnit')
          ?.setValue(
            this.productUnits.find(
              (x) => x.value == existingProduct.deliveryUnitId
            ) || null
          );
      } else {
        childForm
          ?.get('deliveryUnit')
          ?.setValue(
            this.productUnits.find((x) => x.value == product.unitId) || null
          );
        childForm?.get('deliveryRate')?.setValue(product.deliveryRate || 0);
        childForm?.get('availableStock')?.setValue(product.availableStock || 0);
        childForm?.get('deliveryQuantity')?.setValue(0);
      }
    }
  }

  validateDeliveryQuantity() {
    const child = this.productForm;
    const quantity = child?.get('deliveryQuantity')?.value || 0;
    const availableStock = child?.get('availableStock')?.value || 0;

    if (quantity > availableStock) {
      this.toastr.warning(`Only ${availableStock} units available in stock`);
      child?.get('deliveryQuantity')?.setValue(availableStock);
    }
  }

  get productDeliveryDetails() {
    return this.register.get('productDeliveryDetails') as FormArray;
  }

  addToCart() {
    if (!this.productForm.valid) {
      this.toastr.error('Please fill all required fields');
      return;
    }

    const formData = this.productForm.value;
    const cardData: Array<any> = this.productDeliveryDetails.value;
    const existingProduct = cardData.find(
      (x) => x.productId === formData.product.productId
    );

    if (existingProduct) {
      existingProduct.deliveryUnitId =
        formData.deliveryUnit?.value || formData.deliveryUnit;
      existingProduct.deliveryUnitName =
        formData.deliveryUnit?.text || formData.deliveryUnit?.label || '';
      existingProduct.deliveryRate = formData.deliveryRate;
      existingProduct.deliveryQuantity = formData.deliveryQuantity;
      existingProduct.baseQuantity = formData.baseQuantity;
      existingProduct.baseRate = formData.baseRate;
      this.productDeliveryDetails.setValue(cardData);
    } else {
      const item = this.fb.group({
        id: [formData.id],
        productId: [formData.product.productId, [Validators.required]],
        productName: [formData.product.productName, [Validators.required]],
        productDeliveryId: [formData.productDeliveryId],
        deliveryUnitId: [formData.deliveryUnit.value, [Validators.required]],
        deliveryUnitName: [formData.deliveryUnit.label, [Validators.required]],
        deliveryRate: [formData.deliveryRate, [Validators.required]],
        deliveryQuantity: [formData.deliveryQuantity, [Validators.required]],
        baseQuantity: [formData.baseQuantity],
        baseRate: [formData.baseRate],
      });
      this.productDeliveryDetails.push(item);
    }
    this.productForm.reset();
    this.initProductForm();
  }

  removeFromCart(indx: number) {
    this.productDeliveryDetails.removeAt(indx);
  }

  delivery(form: UntypedFormGroup) {
    if (!this.register.valid) {
      this.toastr.error('Please fill all required fields');
      return;
    }

    if (this.productDeliveryDetails.length === 0) {
      this.toastr.error('Please add at least one product');
      return;
    }

    this.isSubmitted = true;
    const formData = { ...form.value };
    formData.customerId = formData.customer?.id;
    const payload: IProductDeliveryRequest = { ...formData };

    if (payload.deliveryNumber !== this.generatedCode) {
      this.toastr.error('Delivery number is mismatched !');
      this.isSubmitted = false;
      return;
    }

    if (formData.id === 0) {
      this.productDeliveryService.create(payload).subscribe({
        next: () => {
          this.toastr.success('Product delivery created successfully');
          this.initFormData();
          this.isSubmitted = false;
        },
        error: () => {
          this.isSubmitted = false;
        },
      });
    } else {
      this.productDeliveryService.update(formData.id, payload).subscribe({
        next: () => {
          this.toastr.success('Product delivery updated successfully');
          this.isSubmitted = false;
        },
        error: () => {
          this.isSubmitted = false;
        },
      });
    }
  }

  addCustomer() {
    const modalRef = this.modalService.open(
      AddCustomerComponent,
      ModalOption.lg
    );
    modalRef.result.then((response: any) => {
      if (response?.success) {
        const obj: ICustomerListResponse = {
          id: response.data.id,
          customerName: response.data.customerName,
          customerCode: response.data.customerCode,
          customerMobile: response.data.customerMobile,
          customerEmail: response.data.customerEmail,
          officePhone: response.data.officePhone,
          address: response.data.address,
          creditLimit: response.data.creditLimit,
          openingBalance: response.data.openingBalance,
          status: response.data.status,
          previousDue: 0,
          isSystemDefault: false,
        };
        this.customers = this.customers.insertThenClone(obj);
      }
    });
  }
}
