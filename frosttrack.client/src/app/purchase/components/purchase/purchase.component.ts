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
import { PurchaseService } from 'app/purchase/services/purchase.service';
import { MessageHub } from '../../../config/message-hub';
import { ModalOption } from '../../../config/modal-option';
import {
  IPurchaseListResponse,
  IPurchaseRequest,
  IPurchaseResponse,
} from 'app/purchase/models/purchase.interface';
import { CodeResponse } from '@core/models/code-response';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { ProductService } from 'app/administration/services/product.service';
import { NgSelectModule } from '@ng-select/ng-select';
import { AddProductComponent } from 'app/administration/components/product/add-product/add-product.component';
import { AddSupplierComponent } from 'app/common/components/supplier/add-supplier/add-supplier.component';
import { SupplierService } from 'app/common/services/supplier.service';
import { ISupplierListResponse } from 'app/common/models/supplier.interface';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { DecimaNumberDirective } from '../../../utils/directives/decimal-number-directive';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-purchase',
  templateUrl: './purchase.component.html',
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
  providers: [PurchaseService, AuthService],
})
export class PurchaseComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IPurchaseListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IPurchaseListResponse[] = [];
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
  suppliers: ISupplierListResponse[] = [];
  supplierLoading = false;

  private editableProductId?: number;
  private productSubject: Subject<number> = new Subject<number>();

  private editableSupplierId?: number;
  private supplierSubject: Subject<number> = new Subject<number>();

  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private purchaseService: PurchaseService,
    private authService: AuthService,
    private branchService: BranchService,
    private productService: ProductService,
    private supplierService: SupplierService,
    private route: ActivatedRoute,
    private layoutService: LayoutService
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
    this.fetchSupplierLookup();
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

    this.supplierSubject.subscribe((value: number) => {
      this.register
        .get('supplier')
        ?.setValue(this.suppliers.find((x) => x.id == value));
    });
  }

  getUserBranch() {
    this.selectedBranch = this.authService.currentBranchId;
  }

  initFormData() {
    this.initProductForm();
    this.register = this.fb.group({
      id: [0],
      invoiceNumber: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      invoiceDate: [new Date().systemFormat(), [Validators.required]],
      supplier: [null, [Validators.required]],
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
      purchaseDetails: this.fb.array([]),
    });
    this.generateCode();
  }
  initProductForm() {
    this.productForm = this.fb.group({
      id: [0],
      purchaseId: [0],
      product: [null, [Validators.required]],
      purchaseRate: [null, [Validators.required]],
      purchaseUnitId: [null, [Validators.required]],
      purchaseQuantity: [null, [Validators.required]],
      purchaseAmount: [null, [Validators.required]],
      bookingRate: [null],
    });
  }
  initExistingFormData(data: IPurchaseResponse) {
    this.initProductForm();
    this.register = this.fb.group({
      id: [data.id],
      invoiceNumber: [data.invoiceNumber, [Validators.required]],
      branchId: [data.branchId, [Validators.required]],
      invoiceDate: [data.invoiceDate.systemFormat(), [Validators.required]],
      supplier: [null, [Validators.required]],
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
      purchaseDetails: this.fb.array([]),
    });
  }
  getExistingData(id: any) {
    this.isLoading = true;
    this.purchaseService.getById(id).subscribe({
      next: (response: IPurchaseResponse) => {
        this.register.setValue({
          id: response.id,
          invoiceNumber: response.invoiceNumber,
          branchId: response.branchId,
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
          purchaseDetails: [],
          supplier:
            this.suppliers.find((x) => x.id == response.supplierId) || null,
        });

        for (const detail of response.purchaseDetails) {
          const item = this.fb.group({
            id: [detail.id],
            productId: [detail.productId, [Validators.required]],
            productName: [detail.product?.productName, [Validators.required]],
            bookingRate: [detail.product?.bookingRate],
            purchaseId: [detail.purchaseId],
            purchaseUnitId: [detail.purchaseUnitId, [Validators.required]],
            purchaseRate: [detail.purchaseRate, [Validators.required]],
            purchaseQuantity: [detail.purchaseQuantity, [Validators.required]],
            purchaseAmount: [detail.purchaseAmount, [Validators.required]],
          });
          this.purchaseDetails.push(item);
        }

        this.editableSupplierId = response.supplierId;
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

  fetchSupplierLookup() {
    this.supplierLoading = true;
    this.supplierService.getList().subscribe({
      next: (response: ISupplierListResponse[]) => {
        this.suppliers = response;
        if (this.editableSupplierId) {
          this.supplierSubject.next(this.editableSupplierId);
        }
        this.supplierLoading = false;
      },
      error: () => {
        this.supplierLoading = false;
      },
    });
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.purchaseService.generateInvoiceNumber().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('invoiceNumber')?.setValue(response.code);
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
      const cardData: Array<any> = this.purchaseDetails.value;
      const existingProduct = cardData.find((x) => x.productId == product.id);
      if (existingProduct) {
        childForm
          ?.get('purchaseUnitId')
          ?.setValue(existingProduct.purchaseUnitId);
        childForm?.get('purchaseRate')?.setValue(existingProduct.purchaseRate);
        childForm
          ?.get('purchaseQuantity')
          ?.setValue(existingProduct.purchaseQuantity);
        childForm?.get('bookingRate')?.setValue(existingProduct.bookingRate);
        childForm
          ?.get('purchaseAmount')
          ?.setValue(existingProduct.purchaseAmount);
      } else {
        childForm?.get('purchaseUnitId')?.setValue(product.defaultUnitId);
        childForm?.get('purchaseRate')?.setValue(product.purchaseRate);
        childForm?.get('purchaseQuantity')?.setValue(0);
        childForm?.get('bookingRate')?.setValue(product.bookingRate);
      }
    }
  }
  setPurchaseAmount() {
    const child = this.productForm;
    child
      ?.get('purchaseAmount')
      ?.setValue(
        child?.get('purchaseQuantity')?.value *
          child?.get('purchaseRate')?.value
      );
  }

  get purchaseDetails() {
    return this.register.get('purchaseDetails') as FormArray;
  }

  addToCart() {
    const formData = this.productForm.value;
    const cardData: Array<any> = this.purchaseDetails.value;
    const existingProduct = cardData.find(
      (x) => x.productId == formData.product.id
    );
    if (existingProduct) {
      existingProduct.purchaseUnitId = formData.purchaseUnitId;
      existingProduct.purchaseRate = formData.purchaseRate;
      existingProduct.purchaseQuantity = formData.purchaseQuantity;
      existingProduct.purchaseAmount = formData.purchaseAmount;
      existingProduct.bookingRate = formData.bookingRate;
      this.purchaseDetails.setValue(cardData);
    } else {
      const item = this.fb.group({
        id: [formData.id],
        productId: [formData.product.id, [Validators.required]],
        productName: [formData.product.productName, [Validators.required]],
        purchaseId: [formData.purchaseId],
        purchaseUnitId: [formData.purchaseUnitId, [Validators.required]],
        purchaseRate: [formData.purchaseRate, [Validators.required]],
        purchaseQuantity: [formData.purchaseQuantity, [Validators.required]],
        purchaseAmount: [formData.purchaseAmount, [Validators.required]],
        bookingRate: [formData.bookingRate],
      });
      // Add the new form group to the FormArray
      this.purchaseDetails.push(item);
    }
    this.productForm.reset();
    this.initProductForm();
    this.calculateSubTotal();
  }

  removeFromCart(indx: number) {
    this.purchaseDetails.removeAt(indx);
    this.calculateSubTotal();
  }
  calculateSubTotal() {
    const cart: any[] = this.purchaseDetails?.value;
    if (cart) {
      const subTotal = cart?.reduce(
        (sum, current) => sum + current.purchaseAmount,
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
  purchase(form: UntypedFormGroup) {
    if (this.register.valid) {
      console.log(this.register.value);
      this.isSubmitted = true;
      const formData = { ...form.value };
      formData.categoryId = formData.productCategory?.value;
      formData.defaultUnitId = formData.defaultUnit?.value;
      formData.supplierId = formData.supplier?.id;
      const payload: IPurchaseRequest = { ...formData };
      if (payload.invoiceNumber != this.generatedCode)
        this.toastr.error('Invoice number is mismatched !');
      if (formData.id == 0) {
        this.purchaseService.create(payload).subscribe({
          next: () => {
            this.initFormData();
            this.isSubmitted = false;
          },
          error: () => {
            this.isSubmitted = false;
          },
        });
      } else {
        this.purchaseService.update(formData.id, payload).subscribe({
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
        this.purchaseService.remove(row.id).subscribe({
          next: (response) => {
            if (response) {
              this.removeRecord(row);
            }
          },
          error: () => {},
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

  addSupplier() {
    const modalRef = this.modalService.open(
      AddSupplierComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
      if (response?.success) {
        const obj: ISupplierListResponse = {
          id: response.data.id,
          supplierName: response.data.supplierName,
          supplierCode: response.data.supplierCode,
          supplierMobile: response.data.supplierMobile,
          supplierEmail: response.data.supplierEmail,
          officePhone: response.data.officePhone,
          address: response.data.address,
          creditLimit: response.data.creditLimit,
          openingBalance: response.data.openingBalance,
          status: response.data.status,
          previousDue: 0,
          isSystemDefault: false,
        };
        this.suppliers = this.suppliers.insertThenClone(obj);
      }
    });
  }
}
