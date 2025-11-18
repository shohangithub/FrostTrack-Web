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
import { DeliveryService } from 'app/product-delivery/services/product-delivery.service';
import {
  IDeliveryListResponse,
  IDeliveryRequest,
  IDeliveryResponse,
  ICustomerStockResponse,
} from 'app/product-delivery/models/product-delivery.interface';
import { CodeResponse } from '@core/models/code-response';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { NgSelectModule } from '@ng-select/ng-select';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { UnitConversionService } from 'app/common/services/unit-conversion.service';
import { BookingService } from 'app/booking/services/booking.service';

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
  providers: [DeliveryService, AuthService, BookingService],
})
export class DeliveryComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IDeliveryListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IDeliveryListResponse[] = [];
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

  customerStockProducts: ICustomerStockResponse[] = [];
  productLoading = false;
  bookings: ILookup<string>[] = [];
  bookingLoading = false;
  productUnits: ILookup<number>[] = [];
  productUnitLoading = false;

  private editableBookingId?: string;
  private bookingSubject: Subject<string> = new Subject<string>();

  private editableProductUnitId?: number;
  private productUnitSubject: Subject<number> = new Subject<number>();

  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private deliveryService: DeliveryService,
    private branchService: BranchService,
    private bookingService: BookingService,
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
    this.fetchBookingLookup();
    this.fetchProductUnitLookup();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.getExistingData(id);

    this.branchSubject.subscribe((value: number) => {
      if (value === 1) {
        this.isMainBranch = true;
        this.register.get('branchId')?.setValue(value);
      }
    });

    this.bookingSubject.subscribe((value: string) => {
      this.register
        .get('booking')
        ?.setValue(this.bookings.find((x) => x.value === value));
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
      id: ['00000000-0000-0000-0000-000000000000'],
      deliveryNumber: ['', [Validators.required]],
      branchId: [this.selectedBranch, [Validators.required]],
      deliveryDate: [new Date().systemFormat(), [Validators.required]],
      booking: [null, [Validators.required]],
      notes: [''],
      chargeAmount: [0, [Validators.required]],
      adjustmentValue: [0],
      discountAmount: [0],
      paidAmount: [0, [Validators.required]],
      deliveryDetails: this.fb.array([]),
    });
    this.generateCode();
  }

  initProductForm() {
    this.productForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      deliveryId: ['00000000-0000-0000-0000-000000000000'],
      product: [null, [Validators.required]],
      chargeAmount: [0, [Validators.required]],
      deliveryUnit: [null, [Validators.required]],
      deliveryQuantity: [null, [Validators.required]],
      baseQuantity: [0],
      adjustmentValue: [0],
      availableStock: [{ value: 0, disabled: true }],
    });
  }

  getExistingData(id: any) {
    this.isLoading = true;
    this.deliveryService.getById(id).subscribe({
      next: (response: IDeliveryResponse) => {
        this.register.setValue({
          id: response.id,
          deliveryNumber: response.deliveryNumber,
          branchId: response.branchId,
          deliveryDate: response.deliveryDate,
          notes: response.notes || '',
          chargeAmount: response.chargeAmount,
          adjustmentValue: response.adjustmentValue,
          discountAmount: response.discountAmount,
          paidAmount: response.paidAmount,
          deliveryDetails: [],
          booking:
            this.bookings.find((x) => x.value === response.bookingId) || null,
        });

        for (const detail of response.deliveryDetails) {
          const item = this.fb.group({
            id: [detail.id],
            bookingDetailId: [detail.bookingDetailId, [Validators.required]],
            productId: [detail.productId, [Validators.required]],
            productName: [detail.productName, [Validators.required]],
            deliveryUnitId: [detail.deliveryUnitId, [Validators.required]],
            deliveryUnitName: [
              detail.deliveryUnitName || '',
              [Validators.required],
            ],
            chargeAmount: [detail.chargeAmount, [Validators.required]],
            deliveryQuantity: [detail.deliveryQuantity, [Validators.required]],
            baseQuantity: [detail.baseQuantity],
            adjustmentValue: [detail.adjustmentValue],
          });
          this.deliveryDetails.push(item);
        }

        this.editableBookingId = response.bookingId;
        this.generatedCode = response.deliveryNumber;

        // Load customer stock after setting booking
        if (response.booking?.customerId) {
          this.loadCustomerStock(response.booking.customerId);
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

  fetchBookingLookup() {
    this.bookingLoading = true;
    this.bookingService.getLookup().subscribe({
      next: (response: ILookup<string>[]) => {
        this.bookings = response;
        if (this.editableBookingId) {
          this.bookingSubject.next(this.editableBookingId);
        }
        this.bookingLoading = false;
      },
      error: () => {
        this.bookingLoading = false;
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

  onBookingChange() {
    const booking = this.register.get('booking')?.value;
    if (booking && booking.customerId) {
      // Note: ILookup doesn't have customerId, we need the full booking object
      // For now, we'll need to handle this differently
      this.productForm.reset();
      this.initProductForm();
    } else {
      this.customerStockProducts = [];
    }
  }

  loadCustomerStock(customerId: number) {
    this.productLoading = true;
    this.deliveryService.getCustomerStockByCustomerId(customerId).subscribe({
      next: (response: ICustomerStockResponse[]) => {
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
    this.deliveryService.generateDeliveryNumber().subscribe({
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
      const cardData: Array<any> = this.deliveryDetails.value;
      const existingProduct = cardData.find(
        (x) => x.bookingDetailId === product.bookingDetailId
      );

      if (existingProduct) {
        childForm
          ?.get('chargeAmount')
          ?.setValue(existingProduct.chargeAmount || 0);
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
        childForm?.get('chargeAmount')?.setValue(product.bookingRate || 0);
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

  get deliveryDetails() {
    return this.register.get('deliveryDetails') as FormArray;
  }

  addToCart() {
    if (!this.productForm.valid) {
      this.toastr.error('Please fill all required fields');
      return;
    }

    const formData = this.productForm.value;
    const cardData: Array<any> = this.deliveryDetails.value;
    const existingProduct = cardData.find(
      (x) => x.bookingDetailId === formData.product.bookingDetailId
    );

    if (existingProduct) {
      existingProduct.deliveryUnitId =
        formData.deliveryUnit?.value || formData.deliveryUnit;
      existingProduct.deliveryUnitName =
        formData.deliveryUnit?.text || formData.deliveryUnit?.label || '';
      existingProduct.chargeAmount = formData.chargeAmount;
      existingProduct.deliveryQuantity = formData.deliveryQuantity;
      existingProduct.baseQuantity = formData.baseQuantity;
      existingProduct.adjustmentValue = formData.adjustmentValue;
      this.deliveryDetails.setValue(cardData);
    } else {
      const item = this.fb.group({
        id: [formData.id],
        bookingDetailId: [
          formData.product.bookingDetailId,
          [Validators.required],
        ],
        productId: [formData.product.productId, [Validators.required]],
        productName: [formData.product.productName, [Validators.required]],
        deliveryUnitId: [formData.deliveryUnit.value, [Validators.required]],
        deliveryUnitName: [formData.deliveryUnit.label, [Validators.required]],
        chargeAmount: [formData.chargeAmount, [Validators.required]],
        deliveryQuantity: [formData.deliveryQuantity, [Validators.required]],
        baseQuantity: [formData.baseQuantity],
        adjustmentValue: [formData.adjustmentValue],
      });
      this.deliveryDetails.push(item);
    }
    this.productForm.reset();
    this.initProductForm();
  }

  removeFromCart(indx: number) {
    this.deliveryDetails.removeAt(indx);
  }

  delivery(form: UntypedFormGroup) {
    if (!this.register.valid) {
      this.toastr.error('Please fill all required fields');
      return;
    }

    if (this.deliveryDetails.length === 0) {
      this.toastr.error('Please add at least one product');
      return;
    }

    this.isSubmitted = true;
    const formData = { ...form.value };
    formData.bookingId = formData.booking?.value;
    const payload: IDeliveryRequest = { ...formData };

    if (payload.deliveryNumber !== this.generatedCode) {
      this.toastr.error('Delivery number is mismatched !');
      this.isSubmitted = false;
      return;
    }

    if (formData.id === '00000000-0000-0000-0000-000000000000') {
      this.deliveryService.create(payload).subscribe({
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
      this.deliveryService.update(formData.id, payload).subscribe({
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
}
