import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
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
import { ITransactionListResponse } from '../../models/transaction.interface';
import { TransactionService } from '../../services/transaction.service';
import { ROLES } from 'app/common/data/settings-data';
import { SwalConfirm } from 'app/theme-config';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-transaction-list',
  templateUrl: './transaction-list.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    DatePipe,
    DecimalPipe,
    CommonModule,
    RouterLink,
  ],
})
export class TransactionListComponent implements OnInit {
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
        this.fetchData();
      });
  }

  fetchData() {
    this.transactionService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<ITransactionListResponse>) => {
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

  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }

  editRow(row: ITransactionListResponse) {
    this.router.navigate(['transaction/edit', row.id]);
  }

  softDeleteRow(row: ITransactionListResponse) {
    Swal.fire({
      title: 'Are you sure you want to soft delete this transaction?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.transactionService.softDelete(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
  }

  restoreRow(row: ITransactionListResponse) {
    Swal.fire({
      title: 'Are you sure you want to restore this transaction?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.transactionService.restore(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
  }

  archiveRow(row: ITransactionListResponse) {
    Swal.fire({
      title: 'Are you sure you want to archive this transaction?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.transactionService.archive(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
  }

  unarchiveRow(row: ITransactionListResponse) {
    Swal.fire({
      title: 'Are you sure you want to unarchive this transaction?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.transactionService.unarchive(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
  }

  delete(row: ITransactionListResponse) {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.transactionService.remove(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
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
        this.transactionService.batchDelete(ids).subscribe({
          next: () => {
            this.selected = [];
            this.isRowSelected = false;
            this.fetchData();
          },
          error: () => {
            // BaseService already handles error toasts via ErrorHandlerService
          },
        });
      }
    });
  }
}
