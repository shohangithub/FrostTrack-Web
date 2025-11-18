import { DatePipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Configuration } from '@config/configuration';
import { MessageHub } from '@config/message-hub';
import { DefaultPagination } from '@config/pagination';
import { PaginationQuery } from '@core/models/pagination-query';
import {
  PaginationResult,
  PagingResponse,
} from '@core/models/pagination-result';
import { LayoutService } from '@core/service/layout.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import {
  DatatableComponent,
  NgxDatatableModule,
  SelectionType,
} from '@swimlane/ngx-datatable';
import { IDeliveryListResponse } from 'app/product-delivery/models/product-delivery.interface';
import { DeliveryService } from 'app/product-delivery/services/product-delivery.service';
import { SwalConfirm } from 'app/theme-config';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-product-delivery-list',
  templateUrl: './product-delivery-list.component.html',
  standalone: true,
  imports: [NgxDatatableModule, DatePipe],
})
export class DeliveryListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  @ViewChild('accordionContainer', { static: true })
  accordionContainer!: ElementRef;

  rows = [];
  expanded: any = {};
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IDeliveryListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IDeliveryListResponse[] = [];
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
    private deliveryService: DeliveryService,
    private router: Router,
    private layoutService: LayoutService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.layoutService.loadCurrentRoute();
  }

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
        this.deliveryService.batchDelete(ids).subscribe({
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
    this.deliveryService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IDeliveryListResponse>) => {
        this.data = response.data;
        this.paging = response.paging;
        this.loadingIndicator = false;
      },
      error: () => {
        this.loadingIndicator = false;
      },
    });
  }

  changePagination(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset;
    this.fetchData();
  }

  onSortring(event: any) {
    const sort = event.sorts[0];
    this.pagination.orderBy = sort.prop;
    this.pagination.isAscending = sort.dir === 'desc' ? false : true;
    this.fetchData();
  }

  editRow(row: any) {
    this.router.navigate(['product-delivery/edit', row.id]);
  }

  delete(row: any) {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.deliveryService.remove(row.id).subscribe({
          next: (response) => {
            if (response) {
              this.removeRecord(row);
              this.toastr.success(MessageHub.DELETE_ONE);
            }
          },
          error: (err: ErrorResponse) => {
            this.toastr.error(formatErrorMessage(err));
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

  expandId: number = 0;
  toggleExpandRow(row: any) {
    if (this.expandId === row.id) this.table.rowDetail.collapseAllRows();
    this.table.rowDetail.toggleExpandRow(row);
    this.table.rowDetail.rowHeight = 100 + row.deliveryDetails.length * 15;
  }

  onDetailToggle(event: any) {
    console.log('Detail Toggled', event);
  }

  printInvoice(row: any) {
    this.router.navigate(['/product-delivery/record', row.id]);
  }
}
