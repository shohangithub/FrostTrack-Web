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
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { SaleReturnService } from 'app/sales/services/sale-return.service';
import {
  ISaleReturnRequest,
  ISaleReturnResponse,
} from 'app/sales/models/sale-return.interface';
import { CodeResponse } from '@core/models/code-response';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { ProductService } from 'app/administration/services/product.service';
import { NgSelectModule } from '@ng-select/ng-select';
import { AddProductComponent } from 'app/administration/components/product/add-product/add-product.component';
import { AddCustomerComponent } from 'app/common/components/customer/add-customer/add-customer.component';
import { CustomerService } from 'app/common/services/customer.service';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { SalesService } from '../../services/sales.service';
import { ISalesListResponse } from '../../models/sales.interface';

@Component({
  selector: 'app-sale-return',
  templateUrl: './sale-return.component.html',
  styleUrls: ['./sale-return.component.sass'],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    NgSelectModule,
  ],
})
export class SaleReturnComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  register!: UntypedFormGroup;
  loadingIndicator = true;
  reorderable = true;
  currentUser: any;
  salesInvoiceNumber = '';
  isDisabled = false;
  isEditing = false;

  branchs: ILookup<number>[] = [];
  customers: ILookup<number>[] = [];
  products: IProductListResponse[] = [];
  salesLookup: ISalesListResponse[] = [];
  selectedSales: any = null;

  constructor(
    private fb: UntypedFormBuilder,
    private toastr: ToastrService,
    private saleReturnService: SaleReturnService,
    private branchService: BranchService,
    private customerService: CustomerService,
    private productService: ProductService,
    private salesService: SalesService,
    private authService: AuthService,
    private layoutService: LayoutService,
    private modalService: NgbModal,
    private route: ActivatedRoute
  ) {
    this.currentUser = this.authService.currentUserValue;
  }

  ngOnInit(): void {
    this.initFormData();
    this.loadDropdownData();
    this.checkEditMode();
  }

  initFormData() {
    this.register = this.fb.group({
      id: [0],
      returnNumber: ['', [Validators.required]],
      returnDate: [
        new Date().toISOString().split('T')[0],
        [Validators.required],
      ],
      salesId: ['', [Validators.required]],
      customerId: ['', [Validators.required]],
      branchId: [this.currentUser?.branchId || '', [Validators.required]],
      subtotal: [0],
      vatPercent: [0],
      vatAmount: [0],
      discountPercent: [0],
      discountAmount: [0],
      otherCost: [0],
      returnAmount: [0],
      reason: ['', [Validators.required]],
      saleReturnDetails: this.fb.array([]),
    });

    this.generateReturnNumber();
  }

  get saleReturnDetails(): FormArray {
    return this.register.get('saleReturnDetails') as FormArray;
  }

  generateReturnNumber() {
    this.saleReturnService.generateReturnNumber().subscribe({
      next: (response: CodeResponse) => {
        this.register.patchValue({
          returnNumber: response.code,
        });
      },
      error: (err: ErrorResponse) => {
        this.toastr.error(formatErrorMessage(err));
      },
    });
  }

  loadDropdownData() {
    // Load branches
    this.branchService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.branchs = response;
      },
    });

    // Load customers - using getWithPagination and convert to lookup format
    const pagination = { page: 1, size: 100 } as any;
    this.customerService.getWithPagination(pagination).subscribe({
      next: (response) => {
        this.customers = response.data.map((customer) => ({
          value: customer.id,
          text: customer.customerName,
        }));
      },
    });

    // Load products - using getWithPagination
    this.productService.getWithPagination(pagination).subscribe({
      next: (response) => {
        this.products = response.data;
      },
    });

    // Load sales for lookup
    this.loadSalesLookup();
  }

  loadSalesLookup() {
    // Load sales with basic pagination to get recent sales
    const pagination = { pageIndex: 0, pageSize: 100 } as any;
    this.salesService.getWithPagination(pagination).subscribe({
      next: (response) => {
        this.salesLookup = response.data;
      },
      error: (err: ErrorResponse) => {
        this.toastr.error(formatErrorMessage(err));
      },
    });
  }

  onSalesChange(salesId: number) {
    if (salesId) {
      this.salesService.getById(salesId).subscribe({
        next: (response) => {
          this.selectedSales = response;
          this.populateReturnFromSales(response);
        },
        error: (err: ErrorResponse) => {
          this.toastr.error(formatErrorMessage(err));
        },
      });
    }
  }

  populateReturnFromSales(sales: any) {
    this.register.patchValue({
      customerId: sales.customerId,
      branchId: sales.branchId,
      vatPercent: sales.vatPercent,
      discountPercent: sales.discountPercent,
    });

    // Clear existing details
    while (this.saleReturnDetails.length !== 0) {
      this.saleReturnDetails.removeAt(0);
    }

    // Add sales details as return details
    sales.salesDetails.forEach((detail: any) => {
      this.addReturnDetail(detail);
    });

    this.calculateTotals();
  }

  addReturnDetail(salesDetail?: any) {
    const returnDetail = this.fb.group({
      id: [0],
      saleReturnId: [0],
      productId: [salesDetail?.productId || '', [Validators.required]],
      returnUnitId: [salesDetail?.salesUnitId || '', [Validators.required]],
      returnQuantity: [0, [Validators.required, Validators.min(0.01)]],
      returnAmount: [0],
      reason: ['', [Validators.required]],
      maxQuantity: [salesDetail?.salesQuantity || 0], // For validation
      rate: [salesDetail?.salesRate || 0],
    });

    this.saleReturnDetails.push(returnDetail);
  }

  removeReturnDetail(index: number) {
    this.saleReturnDetails.removeAt(index);
    this.calculateTotals();
  }

  onQuantityChange(index: number) {
    const detail = this.saleReturnDetails.at(index);
    const quantity = detail.get('returnQuantity')?.value || 0;
    const rate = detail.get('rate')?.value || 0;
    const amount = quantity * rate;

    detail.patchValue({
      returnAmount: amount,
    });

    this.calculateTotals();
  }

  calculateTotals() {
    let subtotal = 0;

    this.saleReturnDetails.controls.forEach((control) => {
      subtotal += control.get('returnAmount')?.value || 0;
    });

    const vatPercent = this.register.get('vatPercent')?.value || 0;
    const discountPercent = this.register.get('discountPercent')?.value || 0;
    const otherCost = this.register.get('otherCost')?.value || 0;

    const vatAmount = (subtotal * vatPercent) / 100;
    const discountAmount = (subtotal * discountPercent) / 100;
    const returnAmount = subtotal + vatAmount + otherCost - discountAmount;

    this.register.patchValue({
      subtotal: subtotal,
      vatAmount: vatAmount,
      discountAmount: discountAmount,
      returnAmount: returnAmount,
    });
  }

  checkEditMode() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== '0') {
      this.isEditing = true;
      this.loadSaleReturnForEdit(+id);
    }
  }

  loadSaleReturnForEdit(id: number) {
    this.saleReturnService.getById(id).subscribe({
      next: (response: ISaleReturnResponse) => {
        this.populateFormForEdit(response);
      },
      error: (err: ErrorResponse) => {
        this.toastr.error(formatErrorMessage(err));
      },
    });
  }

  populateFormForEdit(saleReturn: ISaleReturnResponse) {
    this.register.patchValue({
      id: saleReturn.id,
      returnNumber: saleReturn.returnNumber,
      returnDate: new Date(saleReturn.returnDate).toISOString().split('T')[0],
      salesId: saleReturn.salesId,
      customerId: saleReturn.customerId,
      branchId: saleReturn.branchId,
      subtotal: saleReturn.subtotal,
      vatPercent: saleReturn.vatPercent,
      vatAmount: saleReturn.vatAmount,
      discountPercent: saleReturn.discountPercent,
      discountAmount: saleReturn.discountAmount,
      otherCost: saleReturn.otherCost,
      returnAmount: saleReturn.returnAmount,
      reason: saleReturn.reason,
    });

    // Clear and populate details
    while (this.saleReturnDetails.length !== 0) {
      this.saleReturnDetails.removeAt(0);
    }

    saleReturn.saleReturnDetails.forEach((detail) => {
      const returnDetail = this.fb.group({
        id: [detail.id],
        saleReturnId: [detail.saleReturnId],
        productId: [detail.productId, [Validators.required]],
        returnUnitId: [detail.returnUnitId, [Validators.required]],
        returnQuantity: [detail.returnQuantity, [Validators.required]],
        returnAmount: [detail.returnAmount],
        reason: [detail.reason, [Validators.required]],
        rate: [detail.returnRate],
      });

      this.saleReturnDetails.push(returnDetail);
    });
  }

  saleReturn(form: UntypedFormGroup) {
    if (form.valid && this.saleReturnDetails.length > 0) {
      const payload: ISaleReturnRequest = {
        ...form.value,
        returnDate: new Date(form.value.returnDate),
        saleReturnDetails: this.saleReturnDetails.value,
      };

      if (this.isEditing) {
        this.updateSaleReturn(payload);
      } else {
        this.createSaleReturn(payload);
      }
    } else {
      this.toastr.error(
        'Please fill all required fields and add at least one return item'
      );
    }
  }

  createSaleReturn(payload: ISaleReturnRequest) {
    this.saleReturnService.create(payload).subscribe({
      next: () => {
        this.resetForm();
      },
      error: (err: ErrorResponse) => {
        this.toastr.error(formatErrorMessage(err));
      },
    });
  }

  updateSaleReturn(payload: ISaleReturnRequest) {
    this.saleReturnService.update(payload.id, payload).subscribe({
      next: () => {
        // Handle successful update
      },
      error: (err: ErrorResponse) => {
        this.toastr.error(formatErrorMessage(err));
      },
    });
  }

  resetForm() {
    this.register.reset();
    this.initFormData();
    this.isEditing = false;
  }

  openCustomerModal() {
    const modalRef = this.modalService.open(AddCustomerComponent, {
      size: 'lg',
      backdrop: 'static',
    });
    modalRef.result.then((result) => {
      if (result?.success) {
        this.loadDropdownData();
        this.register.patchValue({
          customerId: result.data.id,
        });
      }
    });
  }

  openProductModal() {
    const modalRef = this.modalService.open(AddProductComponent, {
      size: 'lg',
      backdrop: 'static',
    });
    modalRef.result.then((result) => {
      if (result?.success) {
        this.loadDropdownData();
      }
    });
  }
}
