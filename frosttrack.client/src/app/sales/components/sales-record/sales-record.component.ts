import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgbDate, NgbDatepicker } from '@ng-bootstrap/ng-bootstrap';
import { NgSelectModule } from '@ng-select/ng-select';
import { ISupplierListResponse } from 'app/common/models/supplier.interface';
import { SupplierService } from 'app/common/services/supplier.service';
import { SalesService } from 'app/sales/services/sales.service';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import {
  DateRangeModel,
  DateRangePickerComponent,
} from '../../../shared/date-range-picker/date-range-picker.component';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-sales-record',
  templateUrl: './sales-record.component.html',
  styleUrls: ['./sales-record.component.css'],
  standalone: true,
  providers: [SalesService],
  imports: [
    RouterLink,
    FormsModule,
    ReactiveFormsModule,
    CommonModule,
    NgSelectModule,
    NgbDatepicker,
    DateRangePickerComponent,
  ],
})
export class SalesRecordComponent implements OnInit {
  searchForm!: UntypedFormGroup;
  searchTypes = [
    { value: 'ALL', text: 'All' },
    { value: 'SUPPLIER', text: 'By Supplier' },
    { value: 'CATEGORY', text: 'By Category' },
    { value: 'QUANTITY', text: 'By Quantity' },
    { value: 'USER', text: 'By User' },
  ];

  recordTypes = [
    { value: 'SIMPLE', text: 'Simple' },
    { value: 'DETAIL', text: 'With Detail' }
  ];

  suppliers: ISupplierListResponse[] = [];
  supplierLoading = false;

  constructor(
    private fb: UntypedFormBuilder,
    private toastr: ToastrService,
    private salesService: SalesService,
    private supplierService: SupplierService
  ) {}

  ngOnInit() {
    this.initFormData();
    this.fetchSupplierLookup();
  }

  dateRange = {
    from: { year: 2024, month: 5, day: 1 },
    to: { year: 2024, month: 5, day: 7 },
  };

  initFormData() {
    this.searchForm = this.fb.group({
      searchType: ['ALL', [Validators.required]],
      recordType: ['SIMPLE',[Validators.required]],
      supplier: [null],
      dateRange: [null, [Validators.required]],
      invoiceDate: [new Date().systemFormat(), [Validators.required]],
      // child: this.fb.group({
      //   id: [null],
      //   salesId: [null],
      //   product: [null, [Validators.required]],
      //   salesRate: [null, [Validators.required]],
      //   salesUnitId: [null, [Validators.required]],
      //   salesQuantity: [null, [Validators.required]],
      //   salesAmount: [null, [Validators.required]],
      //   sellingRate: [null],
      // }),
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
  }

  fetchSupplierLookup() {
    this.supplierLoading = true;
    this.supplierService.getList().subscribe({
      next: (response: ISupplierListResponse[]) => {
        this.suppliers = response;
        this.supplierLoading = false;
      },
      error: (err: ErrorResponse) => {
        this.supplierLoading = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  onChangedDateRange(date: NgbDate) {
    const dateFrom = this.searchForm.get('dateFrom')?.value;
    const dateTo = this.searchForm.get('dateTo')?.value;

    if (!dateFrom && !dateTo) {
      this.searchForm.get('dateFrom')?.setValue(date);
    } else if (dateFrom && !dateTo && date.after(dateFrom)) {
      this.searchForm.get('dateTo')?.setValue(date);
    } else {
      this.searchForm.get('dateTo')?.setValue(null);
      this.searchForm.get('dateFrom')?.setValue(date);
    }
  }

  getRecord() {
    if (this.searchForm.valid) {
    }
  }
}
