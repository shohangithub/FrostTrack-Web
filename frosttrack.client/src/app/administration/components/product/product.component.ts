import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { IProductListResponse } from '../../models/product.interface';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { SwalConfirm } from 'app/theme-config';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  PaginationResult,
  PagingResponse,
} from '../../../core/models/pagination-result';
import { PaginationQuery } from '../../../core/models/pagination-query';
import { ProductService } from 'app/administration/services/product.service';
import { DefaultPagination } from '../../../config/pagination';
import { MessageHub } from '../../../config/message-hub';
import { ModalOption } from '../../../config/modal-option';
import { AddProductComponent } from './add-product/add-product.component';
import { Configuration } from '../../../config/configuration';
import { GenerateSingleBarcodeComponent } from './generate-single-barcode/generate-single-barcode.component';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-product',
  templateUrl: './product.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [ProductService],
})
export class ProductComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IProductListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IProductListResponse[] = [];
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;
  @ViewChild(DatatableComponent, { static: false }) table2!: DatatableComponent;
  selection!: SelectionType;
  constructor(
    private modalService: NgbModal,
    private toastr: ToastrService,
    private productService: ProductService,
    private layoutService: LayoutService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.selection = SelectionType.checkbox;
    this.layoutService.loadCurrentRoute();
  }

  // select record using check box
  onSelect({ selected }: { selected: any }) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);

    if (this.selected.length === 0) {
      this.isRowSelected = false;
    } else {
      this.isRowSelected = true;
    }
  }

  deleteSelected() {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        const ids = this.selected.map((x) => x.id);
        this.productService.batchDelete(ids).subscribe({
          next: (response) => {
            if (response) {
              this.selected.forEach((row) => {
                this.removeRecord(row);
              });
              this.deleteRecordSuccess(this.selected.length);
              this.selected = [];
              this.isRowSelected = false;
            }
          },
          error: (err: ErrorResponse) => {
            this.toastr.error(formatErrorMessage(err));
          },
        });
      }
    });
  }

  ngOnInit() {
    this.fetchData();

    //subject call change open text search
    this.searchSubject
      .pipe(
        debounceTime(Configuration.SEARCH_DEBOUNCE_TIME),
        distinctUntilChanged()
      )
      .subscribe((value: any) => {
        this.pagination.openText = value;
        this.fetchData();
      });
  }

  fetchData() {
    this.productService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IProductListResponse>) => {
        this.data = response.data;
        this.paging = response.paging;
        this.loadingIndicator = false;
      },
      error: () => {
        this.loadingIndicator = false;
        // BaseService already handles error toasts via ErrorHandlerService
      },
    });
  }

  changePagination(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset;
    this.fetchData();
  }

  //on sorting
  onSortring(event: any) {
    const sort = event.sorts[0];
    this.pagination.orderBy = sort.prop;
    this.pagination.isAscending = sort.dir === 'desc' ? false : true;
    this.fetchData();
  }

  // add new record
  addRow() {
    const modalRef = this.modalService.open(
      AddProductComponent,
      ModalOption.lg
    );
    modalRef.result.then((response) => {
      if (response?.success) {
        this.fetchData();
      }
    });
  }
  // edit record
  editRow(row: any, rowIndex: number) {
    const modalRef = this.modalService.open(
      AddProductComponent,
      ModalOption.lg
    );
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;
    modalRef.result.then((response) => {
      if (response?.success) {
        const result = response.data;
        this.data = this.data.filter((value, key) => {
          if (value.id == result.id) {
            value.productCode = result.productCode;
            value.productName = result.productName;
            value.customBarcode = result.customBarcode;
            value.categoryId = result.categoryId;
            value.categoryName = result.categoryName;
            value.defaultUnitId = result.defaultUnitId;
            value.unitName = result.unitName;
            value.imageUrl = result.imageUrl;
            value.bookingRate = result.bookingRate;
            value.isActive = result.isActive;
            value.branchId = result.branchId;
          }
          return true;
        });
      }
    });
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
        this.productService.remove(row.id).subscribe({
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

  searchSubject = new Subject<any>();
  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }
  deleteRecordSuccess(count: number) {
    this.toastr.success(count + ' Records Deleted Successfully', '');
  }

  printBarcode(row: IProductListResponse) {
    const modalRef = this.modalService.open(
      GenerateSingleBarcodeComponent,
      ModalOption.lg
    );
    modalRef.componentInstance.data = row;
    modalRef.result.then((response) => {
      if (response?.success) {
        const result = response.data;
      }
    });
  }
}
