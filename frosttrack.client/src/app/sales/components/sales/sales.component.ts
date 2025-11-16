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
import Swal from 'sweetalert2';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { SwalConfirm } from 'app/theme-config';
import { Subject } from 'rxjs';
import { SalesService } from 'app/sales/services/sales.service';
import { MessageHub } from '../../../config/message-hub';
import { ModalOption } from '../../../config/modal-option';
import {
  ISalesListResponse,
  ISalesRequest,
  ISalesResponse,
} from 'app/sales/models/sales.interface';
import { CodeResponse } from '@core/models/code-response';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { ProductService } from 'app/administration/services/product.service';
import { NgSelectModule } from '@ng-select/ng-select';
import { AddProductComponent } from 'app/administration/components/product/add-product/add-product.component';
import { AddCustomerComponent } from 'app/common/components/customer/add-customer/add-customer.component';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';
import {
  IProductListResponse,
  IProductListWithStockResponse,
} from 'app/administration/models/product.interface';
import { DecimaNumberDirective } from '../../../utils/directives/decimal-number-directive';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { SALES_TYPES } from '../../../common/data/settings-data';
import { InvoiceService } from '../../services/invoice.service';

@Component({
  selector: 'app-sales',
  templateUrl: './sales.component.html',
  standalone: true,
  imports: [
    RouterLink,
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
    DecimaNumberDirective,
  ],
  providers: [SalesService],
})
export class SalesComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: ISalesListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: ISalesListResponse[] = [];
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

  products: IProductListWithStockResponse[] = [];
  productLoading = false;
  customers: ICustomerListResponse[] = [];
  customerLoading = false;
  salesTypes = SALES_TYPES;

  private editableProductId?: number;
  private productSubject: Subject<number> = new Subject<number>();

  private editableCustomerId?: number;
  private customerSubject: Subject<number> = new Subject<number>();

  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private salesService: SalesService,
    private authService: AuthService,
    private branchService: BranchService,
    private productService: ProductService,
    private customerService: CustomerService,
    private route: ActivatedRoute,
    private layoutService: LayoutService,
    private invoiceService: InvoiceService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.initFormData();
    this.fetchBranchLookup();
    this.fetchProductLookup();
    this.fetchCustomerLookup();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.getExistingData(id);

    this.branchSubject.subscribe((value: number) => {
      if (value == 1) {
        this.isMainBranch = true;
        this.register.get('branchId')?.setValue(value);
      }
    });

    this.productSubject.subscribe((value: number) => {
      this.register
        .get('child')
        ?.get('product')
        ?.setValue(this.products.find((x) => x.id == value));
    });

    this.customerSubject.subscribe((value: number) => {
      this.register
        .get('customer')
        ?.setValue(this.customers.find((x) => x.id == value));
    });

    this.authService.getCurrentSelectedBranch$.subscribe({
      next: (response: number) => {
        this.selectedBranch = response;
      },
      error: (err) => {},
    });
  }

  initFormData() {
    this.initProductForm();
    this.register = this.fb.group({
      id: [0],
      invoiceNumber: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      invoiceDate: [new Date().systemFormat(), [Validators.required]],
      salesType: [SALES_TYPES.RETAIL, [Validators.required]],
      customer: [null, [Validators.required]],
      subtotal: [0, [Validators.required]],
      vatPercent: [0],
      vatAmount: [0],
      discountPercent: [0],
      discountAmount: [0],
      otherCost: [0],
      invoiceAmount: [0, [Validators.required]],
      paidAmount: [0],
      dueAmount: [0],
      prevDueAmount: [0],
      salesDetails: this.fb.array([]),
    });
    this.generateCode();
  }
  initProductForm() {
    this.productForm = this.fb.group({
      id: [0],
      salesId: [0],
      product: [null, [Validators.required]],
      salesRate: [
        null,
        [
          Validators.required,
          Validators.min(1),
          Validators.pattern(/^[1-9]\d*$/),
        ],
      ],
      salesUnitId: [null, [Validators.required]],
      salesQuantity: [
        null,
        [
          Validators.required,
          Validators.min(1),
          Validators.pattern(/^[1-9]\d*$/),
        ],
      ],
      salesAmount: [
        null,
        [
          Validators.required,
          Validators.min(1),
          Validators.pattern(/^[1-9]\d*$/),
        ],
      ],
    });
  }
  initExistingFormData(data: ISalesResponse) {
    this.initProductForm();
    this.register = this.fb.group({
      id: [data.id],
      invoiceNumber: [data.invoiceNumber, [Validators.required]],
      branchId: [data.branchId, [Validators.required]],
      invoiceDate: [data.invoiceDate.systemFormat(), [Validators.required]],
      salesType: [data.salesType, [Validators.required]],
      customer: [null, [Validators.required]],
      subtotal: [data.subtotal, [Validators.required]],
      vatPercent: [data.vatPercent],
      vatAmount: [data.vatAmount],
      discountPercent: [data.discountPercent],
      discountAmount: [data.discountAmount],
      otherCost: [data.otherCost],
      invoiceAmount: [data.invoiceAmount, [Validators.required]],
      paidAmount: [data.paidAmount],
      dueAmount: [data.invoiceAmount - data.paidAmount],
      prevDueAmount: [0],
      salesDetails: this.fb.array([]),
    });
  }
  getExistingData(id: any) {
    this.isLoading = true;
    this.salesService.getById(id).subscribe({
      next: (response: ISalesResponse) => {
        this.register.setValue({
          id: response.id,
          invoiceNumber: response.invoiceNumber,
          branchId: response.branchId,
          salesType: response.salesType,
          invoiceDate: response.invoiceDate, //.systemFormat(),
          subtotal: response.subtotal,
          vatPercent: response.vatPercent,
          vatAmount: response.vatAmount,
          discountPercent: response.discountPercent,
          discountAmount: response.discountAmount,
          otherCost: response.otherCost,
          invoiceAmount: response.invoiceAmount,
          paidAmount: response.paidAmount,
          dueAmount: response.invoiceAmount - response.paidAmount,
          prevDueAmount: 0,
          salesDetails: [],
          customer:
            this.customers.find((x) => x.id == response.customerId) || null,
        });

        for (let detail of response.salesDetails) {
          const item = this.fb.group({
            id: [detail.id],
            productId: [detail.productId, [Validators.required]],
            productName: [detail.product?.productName, [Validators.required]],
            salesId: [detail.salesId],
            salesUnitId: [detail.salesUnitId, [Validators.required]],
            salesRate: [detail.salesRate, [Validators.required]],
            salesQuantity: [detail.salesQuantity, [Validators.required]],
            salesAmount: [detail.salesAmount, [Validators.required]],
          });
          this.salesDetails.push(item);
        }

        this.editableCustomerId = response.customerId;
        this.generatedCode = response.invoiceNumber;
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
      error: (err: ErrorResponse) => {
        this.loadingIndicator = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  fetchProductLookup() {
    this.productLoading = true;
    this.productService.getListWithStock().subscribe({
      next: (response: IProductListWithStockResponse[]) => {
        this.products = response;
        if (this.editableProductId) {
          this.productSubject.next(this.editableProductId);
        }
        this.productLoading = false;
      },
      error: (err: ErrorResponse) => {
        this.productLoading = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
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
      error: (err: ErrorResponse) => {
        this.customerLoading = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.salesService.generateInvoiceNumber().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('invoiceNumber')?.setValue(response.code);
        this.isGeneratingCode = false;
      },
      error: (err: ErrorResponse) => {
        this.isGeneratingCode = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  setProductDetails() {
    let childForm = this.productForm;
    const product = childForm?.get('product')?.value;
    if (product) {
      const cardData: Array<any> = this.salesDetails.value;
      const existingProduct = cardData.find((x) => x.productId == product.id);
      if (existingProduct) {
        childForm?.get('salesUnitId')?.setValue(existingProduct.salesUnitId);
        childForm?.get('salesRate')?.setValue(existingProduct.salesRate);
        childForm
          ?.get('salesQuantity')
          ?.setValue(existingProduct.salesQuantity);
        childForm?.get('salesAmount')?.setValue(existingProduct.salesAmount);
      } else {
        const salesType = this.register.get('salesType')?.value;
        childForm?.get('salesUnitId')?.setValue(product.defaultUnitId);
        childForm
          ?.get('salesRate')
          ?.setValue(
            salesType == this.salesTypes.RETAIL
              ? product.sellingRate
              : product.wholesalePrice
          );
        childForm?.get('salesQuantity')?.setValue(0);
      }
    }
  }
  setSalesAmount() {
    let child = this.productForm;
    child
      ?.get('salesAmount')
      ?.setValue(
        child?.get('salesQuantity')?.value * child?.get('salesRate')?.value
      );
  }

  get salesDetails() {
    return this.register.get('salesDetails') as FormArray;
  }

  addToCart() {
    const formData = this.productForm.value;
    if (
      !formData.product.isProductAsService &&
      formData.product.currentStock < formData.salesQuantity
    ) {
      Swal.fire({
        title: 'Stock Unavailable !',
        icon: 'warning',
        timer: 5000,
      });
      return;
    }
    const cardData: Array<any> = this.salesDetails.value;
    const existingProduct = cardData.find(
      (x) => x.productId == formData.product.id
    );
    if (existingProduct) {
      existingProduct.salesUnitId = formData.salesUnitId;
      existingProduct.salesRate = formData.salesRate;
      existingProduct.salesQuantity = formData.salesQuantity;
      existingProduct.salesAmount = formData.salesAmount;
      this.salesDetails.setValue(cardData);
    } else {
      const item = this.fb.group({
        id: [formData.id],
        productId: [formData.product.id, [Validators.required]],
        productName: [formData.product.productName, [Validators.required]],
        salesId: [formData.salesId],
        salesUnitId: [formData.salesUnitId, [Validators.required]],
        salesRate: [formData.salesRate, [Validators.required]],
        salesQuantity: [formData.salesQuantity, [Validators.required]],
        salesAmount: [formData.salesAmount, [Validators.required]],
      });
      // Add the new form group to the FormArray
      this.salesDetails.push(item);
    }
    this.productForm.reset();
    this.initProductForm();
    this.calculateSubTotal();
  }

  removeFromCart(indx: number) {
    this.salesDetails.removeAt(indx);
    this.calculateSubTotal();
  }
  calculateSubTotal() {
    const cart: any[] = this.salesDetails?.value;
    if (cart) {
      const subTotal = cart?.reduce(
        (sum, current) => sum + current.salesAmount,
        0
      );

      this.register.get('subtotal')?.setValue(subTotal);
    }
  }
  setInvoiceTotal() {
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
      .get('invoiceAmount')
      ?.setValue(subtotal + vatAmount + otherCost - discountAmount);
  }

  // add new record
  sales(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      formData.categoryId = formData.productCategory?.value;
      formData.defaultUnitId = formData.defaultUnit?.value;
      formData.customerId = formData.customer?.id;
      const payload: ISalesRequest = { ...formData };
      if (payload.invoiceNumber != this.generatedCode)
        this.toastr.error('Invoice number is mismatched !');
      if (formData.id == 0) {
        this.salesService.create(payload).subscribe({
          next: (response: ISalesResponse) => {
            this.fetchProductLookup();
            this.initFormData();
            this.isSubmitted = false;
          },
          error: () => {
            this.isSubmitted = false;
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      } else {
        this.salesService.update(formData.id, payload).subscribe({
          next: (response: ISalesResponse) => {
            this.isSubmitted = false;
          },
          error: () => {
            this.isSubmitted = false;
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    }
  }

  // Complete sale and print invoice
  salesAndPrint(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      formData.categoryId = formData.productCategory?.value;
      formData.defaultUnitId = formData.defaultUnit?.value;
      formData.customerId = formData.customer?.id;
      const payload: ISalesRequest = { ...formData };
      if (payload.invoiceNumber != this.generatedCode)
        this.toastr.error('Invoice number is mismatched !');

      if (formData.id == 0) {
        this.salesService.create(payload).subscribe({
          next: (response: ISalesResponse) => {
            this.fetchProductLookup();
            this.initFormData();
            this.isSubmitted = false;
            // Generate and print invoice
            this.invoiceService.printInvoice(response);
            this.toastr.success('Sale completed and invoice printed!');
          },
          error: () => {
            this.isSubmitted = false;
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      } else {
        this.salesService.update(formData.id, payload).subscribe({
          next: (response: ISalesResponse) => {
            this.isSubmitted = false;
            // Generate and print invoice
            this.invoiceService.printInvoice(response);
            this.toastr.success('Sale updated and invoice printed!');
          },
          error: () => {
            this.isSubmitted = false;
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    }
  }

  // Generate and download invoice PDF
  generateInvoice(salesData: ISalesResponse) {
    this.invoiceService.generateInvoice(salesData);
  }
  // delete single row
  delete(row: any) {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.salesService.remove(row.id).subscribe({
          next: (response) => {
            if (response) {
              this.removeRecord(row);
              this.deleteRecordSuccess(1);
            }
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
  }

  private removeRecord(row: any) {
    this.data = this.arrayRemove(this.data, row.id);
  }
  private arrayRemove(array: any[], id: any) {
    return array.filter(function (element) {
      return element.id !== id;
    });
  }

  deleteRecordSuccess(count: number) {
    this.toastr.success(count + ' Records Deleted Successfully', '');
  }

  addProduct() {
    const modalRef = this.modalService.open(
      AddProductComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
      if (response?.success) {
        const result = response.data;
        const obj: IProductListWithStockResponse = {
          id: result.id,
          productCode: result.productCode,
          productName: result.productName,
          customBarcode: result.customBarcode,
          categoryId: result.categoryId,
          categoryName: result.categoryName,
          defaultUnitId: result.defaultUnitId,
          unitName: result.unitName,
          imageUrl: result.imageUrl,
          isRawMaterial: result.isRawMaterial,
          isFinishedGoods: result.isFinishedGoods,
          reOrederLevel: result.reOrederLevel,
          purchaseRate: result.purchaseRate,
          sellingRate: result.sellingRate,
          wholesalePrice: result.wholesalePrice,
          vatPercent: result.vatPercent,
          isProductAsService: result.isProductAsService,
          isActive: result.isActive,
          status: result.status,
          branchId: result.branchId,
          productAs: result.productAs,
          currentStock: null,
          lastPurchaseRate: null,
          stockUnit: null,
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

  lastPurchaseRateIn(control: any) {
    control.target.type = 'text';
  }

  lastPurchaseRateOut(control: any) {
    control.target.type = 'password';
  }
}
