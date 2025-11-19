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
import { BookingService } from 'app/booking/services/booking.service';
import {
  IBookingListResponse,
  IBookingRequest,
  IBookingResponse,
} from 'app/booking/models/booking.interface';
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
  selector: 'app-booking',
  templateUrl: './booking.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
  ],
  providers: [BookingService, AuthService],
})
export class BookingComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IBookingListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IBookingListResponse[] = [];
  branchs: ILookup<number>[] = [];
  selectedBranch!: number;

  register!: UntypedFormGroup;
  productForm!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  isEditing = false;
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
  billTypes = [
    { label: 'Hourly', value: 'HOURLY' },
    { label: 'Daily', value: 'DAILY' },
    { label: 'Weekly', value: 'WEEKLY' },
    { label: 'Monthly', value: 'MONTHLY' },
    { label: 'Yearly', value: 'YEARLY' },
  ];

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
    private BookingService: BookingService,
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
        .get('bookingUnit')
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
      id: ['00000000-0000-0000-0000-000000000000'],
      bookingNumber: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      bookingDate: [new Date().systemFormat(), [Validators.required]],
      customer: [null, [Validators.required]],
      notes: [''],
      BookingDetails: this.fb.array([]),
    });

    this.generateCode();
  }

  initProductForm() {
    this.productForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      bookingId: ['00000000-0000-0000-0000-000000000000'],
      product: [null, [Validators.required]],
      bookingRate: [null, [Validators.required]],
      bookingUnit: [null, [Validators.required]],
      billType: ['MONTHLY', [Validators.required]],
      bookingQuantity: [null, [Validators.required]],
    });
  }

  getExistingData(id: any) {
    this.isLoading = true;
    this.isEditing = true;
    this.BookingService.getById(id).subscribe({
      next: (response: IBookingResponse) => {
        this.register.setValue({
          id: response.id,
          bookingNumber: response.bookingNumber,
          branchId: response.branchId,
          bookingDate: new Date(response.bookingDate).systemFormat(),
          notes: response.notes || '',
          BookingDetails: [],
          customer:
            this.customers.find((x) => x.id === response.customerId) || null,
        });

        for (const detail of response.bookingDetails) {
          const item = this.fb.group({
            id: [detail.id],
            productId: [detail.productId, [Validators.required]],
            productName: [detail.product?.productName, [Validators.required]],
            bookingId: [detail.bookingId],
            bookingUnitId: [detail.bookingUnitId, [Validators.required]],
            bookingUnitName: [
              detail.bookingUnit?.unitName || 'N/A',
              [Validators.required],
            ],
            billType: [detail.billType, [Validators.required]],
            bookingRate: [detail.bookingRate, [Validators.required]],
            bookingQuantity: [detail.bookingQuantity, [Validators.required]],
            baseQuantity: [detail.baseQuantity],
            baseRate: [detail.baseRate],
          });
          this.BookingDetails.push(item);
        }

        this.editableCustomerId = response.customerId;
        this.generatedCode = response.bookingNumber;
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
    this.BookingService.generateBookingNumber().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        if (!this.isEditing) {
          this.register.get('bookingNumber')?.setValue(response.code);
        }
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
      const cardData: Array<any> = this.BookingDetails.value;
      const existingProduct = cardData.find((x) => x.productId === product.id);
      if (existingProduct) {
        childForm
          ?.get('bookingRate')
          ?.setValue(existingProduct.bookingRate || 0);
        childForm
          ?.get('bookingQuantity')
          ?.setValue(existingProduct.bookingQuantity);
        childForm
          ?.get('bookingUnit')
          ?.setValue(
            this.productUnits.find(
              (x) => x.value == existingProduct.bookingUnitId
            ) || null
          );
        childForm
          ?.get('billType')
          ?.setValue(existingProduct.billType || 'MONTHLY');
      } else {
        childForm
          ?.get('bookingUnit')
          ?.setValue(
            this.productUnits.find((x) => x.value == product.defaultUnitId) ||
              null
          );
        childForm?.get('bookingRate')?.setValue(product.bookingRate || 0);
        childForm?.get('bookingQuantity')?.setValue(null);
        childForm?.get('billType')?.setValue('MONTHLY');
      }
    }
  }

  getCalculatedAmount(): number {
    const rate = this.productForm?.get('bookingRate')?.value || 0;
    const quantity = this.productForm?.get('bookingQuantity')?.value || 0;
    return rate * quantity;
  }

  getTotalQuantity(): number {
    const items = this.BookingDetails.value || [];
    return items.reduce(
      (sum: number, item: any) => sum + (item.bookingQuantity || 0),
      0
    );
  }

  getTotalAmount(): number {
    const items = this.BookingDetails.value || [];
    return items.reduce(
      (sum: number, item: any) =>
        sum + (item.bookingQuantity || 0) * (item.bookingRate || 0),
      0
    );
  }

  get BookingDetails() {
    return this.register.get('BookingDetails') as FormArray;
  }

  addToCart() {
    const formData = this.productForm.value;
    const cardData: Array<any> = this.BookingDetails.value;
    const existingProduct = cardData.find(
      (x) => x.productId === formData.product.id
    );
    if (existingProduct) {
      existingProduct.bookingUnitId =
        formData.bookingUnit?.value || formData.bookingUnit;
      existingProduct.bookingUnitName =
        formData.bookingUnit?.text || formData.bookingUnit;
      existingProduct.bookingRate = formData.bookingRate;
      existingProduct.bookingQuantity = formData.bookingQuantity;
      existingProduct.billType = formData.billType;
      this.BookingDetails.setValue(cardData);
    } else {
      const item = this.fb.group({
        id: [formData.id],
        productId: [formData.product.id, [Validators.required]],
        productName: [formData.product.productName, [Validators.required]],
        bookingId: [formData.bookingId],
        bookingUnitId: [formData.bookingUnit.value, [Validators.required]],
        bookingUnitName: [formData.bookingUnit.text, [Validators.required]],
        billType: [formData.billType, [Validators.required]],
        bookingRate: [formData.bookingRate, [Validators.required]],
        bookingQuantity: [formData.bookingQuantity, [Validators.required]],
      });
      this.BookingDetails.push(item);
    }
    this.productForm.reset();
    this.initProductForm();
  }

  removeFromCart(indx: number) {
    this.BookingDetails.removeAt(indx);
  }

  purchase(form: UntypedFormGroup) {
    console.log(this.register);
    if (this.register.valid) {
      this.isSubmitted = true;
      const formData = { ...form.value };
      formData.customerId = formData.customer?.id;
      const payload: IBookingRequest = { ...formData };
      if (payload.bookingNumber !== this.generatedCode) {
        this.toastr.error('Booking number is mismatched !');
        this.isSubmitted = false;
        return;
      }
      if (formData.id === '00000000-0000-0000-0000-000000000000') {
        this.BookingService.create(payload).subscribe({
          next: () => {
            this.toastr.success('Booking created successfully');
            this.initFormData();
            this.isSubmitted = false;
          },
          error: () => {
            this.isSubmitted = false;
          },
        });
      } else {
        this.BookingService.update(formData.id, payload).subscribe({
          next: () => {
            this.toastr.success('Booking updated successfully');
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
