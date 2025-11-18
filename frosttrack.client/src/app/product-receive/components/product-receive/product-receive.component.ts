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
import { ProductReceiveService } from 'app/product-receive/services/product-receive.service';
import {
  IProductReceiveListResponse,
  IProductReceiveRequest,
  IProductReceiveResponse,
} from 'app/product-receive/models/product-receive.interface';
import { CodeResponse } from '@core/models/code-response';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { ProductService } from 'app/administration/services/product.service';
import { NgSelectModule } from '@ng-select/ng-select';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { ModalOption } from '../../../config/modal-option';
import { AddProductComponent } from 'app/administration/components/product/add-product/add-product.component';
import { AddCustomerComponent } from 'app/common/components/customer/add-customer/add-customer.component';
import { AddBaseUnitComponent } from 'app/common/components/base-unit/add-base-unit/add-base-unit.component';
import { UnitConversionService } from 'app/common/services/unit-conversion.service';

@Component({
  selector: 'app-product-receive',
  templateUrl: './product-receive.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
  ],
  providers: [ProductReceiveService, AuthService],
})
export class ProductReceiveComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IProductReceiveListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IProductReceiveListResponse[] = [];
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

  products: IProductListResponse[] = [];
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
    private productReceiveService: ProductReceiveService,
    private branchService: BranchService,
    private productService: ProductService,
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
    this.fetchProductLookup();
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
        ?.setValue(this.products.find((x) => x.id === value));
    });

    this.customerSubject.subscribe((value: number) => {
      this.register
        .get('customer')
        ?.setValue(this.customers.find((x) => x.id === value));
    });

    this.productUnitSubject.subscribe((value: number) => {
      this.productForm
        .get('receiveUnit')
        ?.setValue(this.productUnits.find((x) => x.value == value));
    });
  }

  getUserBranch() {
    this.selectedBranch = this.authService.currentBranchId;
  }

  initFormData() {
    this.initProductForm();

    //{  "customer": { "id": 1, "customerName": "General", "customerCode": "", "customerBarcode": "", "customerMobile": null, "customerEmail": null, "officePhone": null, "address": null, "imageUrl": "", "creditLimit": 0, "previousDue": 0, "openingBalance": 0, "isSystemDefault": true, "status": "Active" }, "subtotal": 750, "vatAmount": 0, "discountPercent": 0, "discountAmount": 0, "otherCost": 0, "totalAmount": 750, "paidAmount": 750, "dueAmount": 0, "prevDueAmount": 0,  }

    this.register = this.fb.group({
      id: [0],
      receiveNumber: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      receiveDate: [new Date().systemFormat(), [Validators.required]],
      customer: [null, [Validators.required]],
      subtotal: [0, [Validators.required]],
      vatAmount: [0],
      discountPercent: [0],
      discountAmount: [0],
      otherCost: [0],
      totalAmount: [0, [Validators.required]],
      paidAmount: [0],
      dueAmount: [0],
      prevDueAmount: [0],
      productReceiveDetails: this.fb.array([]),
    });
    this.generateCode();
  }

  initProductForm() {
    this.productForm = this.fb.group({
      id: [0],
      productReceiveId: [0],
      product: [null, [Validators.required]],
      bookingRate: [null, [Validators.required]],
      receiveUnit: [null, [Validators.required]],
      receiveQuantity: [null, [Validators.required]],
      receiveAmount: [null, [Validators.required]],
    });
  }

  getExistingData(id: any) {
    this.isLoading = true;
    this.productReceiveService.getById(id).subscribe({
      next: (response: IProductReceiveResponse) => {
        this.register.setValue({
          id: response.id,
          receiveNumber: response.receiveNumber,
          branchId: response.branchId,
          receiveDate: response.receiveDate,
          subtotal: response.subtotal,
          vatAmount: response.vatAmount,
          discountPercent: response.discountPercent,
          discountAmount: response.discountAmount,
          otherCost: response.otherCost,
          totalAmount: response.totalAmount,
          paidAmount: response.paidAmount,
          dueAmount: response.totalAmount - response.paidAmount,
          prevDueAmount: 0,
          productReceiveDetails: [],
          customer:
            this.customers.find((x) => x.id === response.customerId) || null,
        });

        for (const detail of response.productReceiveDetails) {
          const item = this.fb.group({
            id: [detail.id],
            productId: [detail.productId, [Validators.required]],
            productName: [detail.product?.productName, [Validators.required]],
            productReceiveId: [detail.productReceiveId],
            receiveUnitId: [detail.receiveUnitId, [Validators.required]],
            // receiveUnitName: [
            //   detail.receiveUnit?.unitName || '',
            //   [Validators.required],
            // ],
            bookingRate: [detail.bookingRate, [Validators.required]],
            receiveQuantity: [detail.receiveQuantity, [Validators.required]],
            receiveAmount: [detail.receiveAmount, [Validators.required]],
          });
          this.productReceiveDetails.push(item);
        }

        this.editableCustomerId = response.customerId;
        this.generatedCode = response.receiveNumber;
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

  fetchProductLookup() {
    this.productLoading = true;
    this.productService.getProductsWithoutService().subscribe({
      next: (response: IProductListResponse[]) => {
        this.products = response;
        if (this.editableProductId) {
          this.productSubject.next(this.editableProductId);
        }
        this.productLoading = false;
      },
      error: () => {
        this.productLoading = false;
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

  generateCode() {
    this.isGeneratingCode = true;
    this.productReceiveService.generateReceiveNumber().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('receiveNumber')?.setValue(response.code);
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
      const cardData: Array<any> = this.productReceiveDetails.value;
      const existingProduct = cardData.find((x) => x.productId === product.id);
      if (existingProduct) {
        childForm
          ?.get('bookingRate')
          ?.setValue(existingProduct.bookingRate || 0);
        childForm
          ?.get('receiveQuantity')
          ?.setValue(existingProduct.receiveQuantity);
        childForm
          ?.get('receiveAmount')
          ?.setValue(existingProduct.receiveAmount);

        childForm
          ?.get('receiveUnit')
          ?.setValue(
            this.productUnits.find(
              (x) => x.value == existingProduct.receiveUnitId
            ) || null
          );
      } else {
        childForm
          ?.get('receiveUnit')
          ?.setValue(
            this.productUnits.find((x) => x.value == product.defaultUnitId) ||
              null
          );
        childForm?.get('bookingRate')?.setValue(product.bookingRate || 0);
        childForm?.get('receiveQuantity')?.setValue(0);
      }
    }
  }

  setReceiveAmount() {
    const child = this.productForm;
    child
      ?.get('receiveAmount')
      ?.setValue(
        child?.get('receiveQuantity')?.value * child?.get('bookingRate')?.value
      );
  }

  get productReceiveDetails() {
    return this.register.get('productReceiveDetails') as FormArray;
  }

  addToCart() {
    const formData = this.productForm.value;
    const cardData: Array<any> = this.productReceiveDetails.value;
    const existingProduct = cardData.find(
      (x) => x.productId === formData.product.id
    );
    if (existingProduct) {
      existingProduct.receiveUnitId =
        formData.receiveUnit?.value || formData.receiveUnit;
      // existingProduct.receiveUnitName =
      //   formData.receiveUnit?.text || formData.receiveUnit?.label || '';
      existingProduct.bookingRate = formData.bookingRate;
      existingProduct.receiveQuantity = formData.receiveQuantity;
      existingProduct.receiveAmount = formData.receiveAmount;
      this.productReceiveDetails.setValue(cardData);
    } else {
      const item = this.fb.group({
        id: [formData.id],
        productId: [formData.product.id, [Validators.required]],
        productName: [formData.product.productName, [Validators.required]],
        productReceiveId: [formData.productReceiveId],
        receiveUnitId: [formData.receiveUnit.value, [Validators.required]],
        //receiveUnitName: [formData.receiveUnit.label, [Validators.required]],
        bookingRate: [formData.bookingRate, [Validators.required]],
        receiveQuantity: [formData.receiveQuantity, [Validators.required]],
        receiveAmount: [formData.receiveAmount, [Validators.required]],
      });
      this.productReceiveDetails.push(item);
    }
    this.productForm.reset();
    this.initProductForm();
    this.calculateSubTotal();
  }

  removeFromCart(indx: number) {
    this.productReceiveDetails.removeAt(indx);
    this.calculateSubTotal();
  }

  calculateSubTotal() {
    const cart: any[] = this.productReceiveDetails?.value;
    if (cart) {
      const subTotal = cart?.reduce(
        (sum, current) => sum + current.receiveAmount,
        0
      );
      this.register.get('subtotal')?.setValue(subTotal);
    }
  }

  setReceiveTotal() {
    let subtotal: number = parseFloat(this.register.get('subtotal')?.value);
    let vatAmount: number = parseFloat(this.register.get('vatAmount')?.value);
    let discountAmount: number = parseFloat(
      this.register.get('discountAmount')?.value
    );
    let otherCost: number = parseFloat(this.register.get('otherCost')?.value);
    subtotal = isNaN(subtotal) ? 0 : subtotal;
    vatAmount = isNaN(vatAmount) ? 0 : vatAmount;
    discountAmount = isNaN(discountAmount) ? 0 : discountAmount;
    otherCost = isNaN(otherCost) ? 0 : otherCost;
    this.register
      .get('totalAmount')
      ?.setValue(subtotal + vatAmount + otherCost - discountAmount);
  }

  purchase(form: UntypedFormGroup) {
    console.log(this.register);
    if (this.register.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      formData.customerId = formData.customer?.id;
      const payload: IProductReceiveRequest = { ...formData };
      if (payload.receiveNumber !== this.generatedCode)
        this.toastr.error('Receive number is mismatched !');
      if (formData.id === 0) {
        this.productReceiveService.create(payload).subscribe({
          next: () => {
            this.initFormData();
            this.isSubmitted = false;
          },
          error: () => {
            this.isSubmitted = false;
          },
        });
      } else {
        this.productReceiveService.update(formData.id, payload).subscribe({
          next: () => {
            this.isSubmitted = false;
          },
          error: () => {
            this.isSubmitted = false;
          },
        });
      }
    }
  }

  addProduct() {
    const modalRef = this.modalService.open(
      AddProductComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
      if (response?.success) {
        const result = response.data;
        const obj: IProductListResponse = {
          id: result.id,
          productCode: result.productCode,
          productName: result.productName,
          customBarcode: result.customBarcode,
          categoryId: result.categoryId,
          categoryName: result.categoryName,
          defaultUnitId: result.defaultUnitId,
          unitName: result.unitName,
          imageUrl: result.imageUrl,
          bookingRate: result.bookingRate,
          isActive: result.isActive,
          status: result.status,
          branchId: result.branchId,
        };
        this.products = this.products.insertThenClone(obj);
      }
    });
  }

  addCustomer() {
    const modalRef = this.modalService.open(
      AddCustomerComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
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
}
