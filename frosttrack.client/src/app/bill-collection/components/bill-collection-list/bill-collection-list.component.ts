import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Configuration } from '@config/configuration';
import { MessageHub } from '@config/message-hub';
import { DefaultPagination } from '@config/pagination';
import { PaginationQuery } from '@core/models/pagination-query';
import {
  PaginationResult,
  PagingResponse,
} from '@core/models/pagination-result';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import { ITransactionListResponse } from 'app/transaction/models/transaction.interface';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { ROLES } from 'app/common/data/settings-data';
import { SwalConfirm } from 'app/theme-config';
import { formatErrorMessage } from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-bill-collection-list',
  templateUrl: './bill-collection-list.component.html',
  standalone: true,
  imports: [NgxDatatableModule, DatePipe, DecimalPipe, CommonModule],
})
export class BillCollectionListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  scrollBarHorizontal = window.innerWidth < 1200;
  data: ITransactionListResponse[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  reorderable = true;
  selected: ITransactionListResponse[] = [];
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;
  canEdit: boolean = false;
  canDelete: boolean = false;
  searchSubject = new Subject<any>();
  selection = SelectionType.checkbox;

  constructor(
    private router: Router,
    private toastr: ToastrService,
    private authService: AuthService,
    private layoutService: LayoutService,
    private transactionService: TransactionService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.layoutService.loadCurrentRoute();
    this.setPermissions();
  }

  private setPermissions() {
    const roles = this.authService.getUserRoles();
    if (roles.includes(ROLES.SUPERADMIN) || roles.includes(ROLES.ADMIN)) {
      this.canEdit = true;
      this.canDelete = true;
    }
  }

  onSelect({ selected }: { selected: any }) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);
    this.isRowSelected = this.selected.length > 0;
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
        this.pagination.pageIndex = DefaultPagination.PAGEINDEX;
        this.fetchData();
      });
  }

  fetchData() {
    this.loadingIndicator = true;

    // For bill collection, we don't need special filtering - just get all transactions with pagination
    this.transactionService.getWithPagination(this.pagination).subscribe({
      next: (result: PaginationResult<ITransactionListResponse>) => {
        // Filter client-side for BILL_COLLECTION type
        this.data = result.data.filter(
          (t) => t.transactionType === 'BILL_COLLECTION'
        );
        this.paging = result.paging;
        this.loadingIndicator = false;
      },
      error: (err) => {
        this.loadingIndicator = false;
        const errorMessage = formatErrorMessage(err);
        this.toastr.error(errorMessage, 'Load Error');
      },
    });
  }

  onSearch(event: any) {
    this.searchSubject.next(event.target.value);
  }

  add() {
    this.router.navigate(['/bill-collection/add']);
  }

  edit(id: string) {
    this.router.navigate(['/bill-collection/edit', id]);
  }

  view(id: string) {
    this.router.navigate([
      '/transaction/receipt-print',
      id,
      'bill-collection-list',
    ]);
  }

  print(id: string) {
    this.router.navigate([
      '/transaction/receipt-print',
      id,
      'bill-collection-list',
    ]);
  }

  delete(id: string) {
    Swal.fire({
      title: 'Confirm Delete',
      text: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No',
    }).then((result) => {
      if (result.value) {
        this.transactionService.remove(id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: (err) => {
            const errorMessage = formatErrorMessage(err);
            this.toastr.error(errorMessage, 'Delete Error');
          },
        });
      }
    });
  }

  deleteMultiple() {
    if (this.selected.length === 0) {
      this.toastr.warning('Please select items to delete');
      return;
    }

    Swal.fire({
      title: 'Confirm Delete',
      text: `${MessageHub.DELETE_CONFIRM} ${this.selected.length} item(s)?`,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
      cancelButtonText: 'No',
    }).then((result) => {
      if (result.value) {
        const ids = this.selected.map((item) => item.id);
        this.transactionService.batchDelete(ids).subscribe({
          next: () => {
            this.selected = [];
            this.isRowSelected = false;
            this.fetchData();
          },
          error: (err) => {
            const errorMessage = formatErrorMessage(err);
            this.toastr.error(errorMessage, 'Delete Error');
          },
        });
      }
    });
  }

  setPage(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset + 1;
    this.fetchData();
  }

  onSort(event: any) {
    this.pagination.orderBy = event.sorts[0].prop;
    this.pagination.isAscending = event.sorts[0].dir === 'asc';
    this.fetchData();
  }

  getPaymentMethodLabel(method: string): string {
    const methods: { [key: string]: string } = {
      CASH: 'Cash',
      BANK_TRANSFER: 'Bank Transfer',
      CHEQUE: 'Cheque',
      MOBILE_BANKING: 'Mobile Banking',
      CARD: 'Card',
      OTHER: 'Other',
    };
    return methods[method] || method;
  }
}
