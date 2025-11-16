import { DatePipe } from '@angular/common';
import {
  Component,
  ElementRef,
  OnInit,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
import { ISalesListResponse } from 'app/sales/models/sales.interface';
import { SalesService } from 'app/sales/services/sales.service';
import { SwalConfirm } from 'app/theme-config';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import Swal from 'sweetalert2';
import { InvoiceService } from '../../services/invoice.service';

@Component({
  selector: 'app-sales-invoice-list',
  templateUrl: './sales-invoice-list.component.html',
  standalone: true,
  imports: [NgxDatatableModule, DatePipe],
})
export class SalesInvoiceListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  @ViewChild('accordionContainer', { static: true })
  accordionContainer!: ElementRef;

  rows = [];
  expanded: any = {};
  scrollBarHorizontal = window.innerWidth < 1200;
  data: ISalesListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: ISalesListResponse[] = [];
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
    private salesService: SalesService,
    private router: Router,
    private layoutService: LayoutService,
    private invoiceService: InvoiceService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
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
        this.salesService.batchDelete(ids).subscribe({
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
    this.salesService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<ISalesListResponse>) => {
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

  // edit record
  editRow(row: any, rowIndex: number) {
    this.router.navigate(['sales/edit', row.id]);
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
              this.toastr.success(MessageHub.DELETE_ONE);
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

  expandId: number = 0;
  toggleExpandRow(row: any) {
    if (this.expandId == row.id) this.table.rowDetail.collapseAllRows();
    this.table.rowDetail.toggleExpandRow(row);
    this.table.rowDetail.rowHeight = 100 + row.salesDetails.length * 15;
  }

  onDetailToggle(event: any) {
    console.log('Detail Toggled', event);
  }

  printInvoice(row: ISalesListResponse) {
    // Get full sales details and print invoice
    this.salesService.getById(row.id).subscribe({
      next: (response) => {
        this.invoiceService.smartPrint(response, 'pdf');
        this.toastr.success('Invoice sent to printer!');
      },
      error: () => {
        this.toastr.error('Failed to retrieve invoice details');
      },
    });
  }

  downloadInvoice(row: ISalesListResponse) {
    // Get full sales details and generate PDF download
    this.salesService.getById(row.id).subscribe({
      next: (response) => {
        this.invoiceService.generateInvoice(response);
        this.toastr.success('Invoice PDF generated!');
      },
      error: () => {
        this.toastr.error('Failed to retrieve invoice details');
      },
    });
  }
}
