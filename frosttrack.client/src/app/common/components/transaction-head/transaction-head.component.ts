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
import { RouterLink } from '@angular/router';
import { ITransactionHeadListResponse } from '../../models/transaction-head.interface';
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
import { TransactionHeadService } from 'app/common/services/transaction-head.service';
import { DefaultPagination } from '../../../config/pagination';
import { MessageHub } from '../../../config/message-hub';
import { Configuration } from '../../../config/configuration';
import { ModalOption } from '../../../config/modal-option';
import { LayoutService } from '@core/service/layout.service';
import { AddTransactionHeadComponent } from './add-transaction-head/add-transaction-head.component';

@Component({
  selector: 'app-transaction-head',
  templateUrl: './transaction-head.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    RouterLink,
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [TransactionHeadService],
})
export class TransactionHeadComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: ITransactionHeadListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: ITransactionHeadListResponse[] = [];
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
    private transactionHeadService: TransactionHeadService,
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
        this.transactionHeadService.batchDelete(ids).subscribe({
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
    this.transactionHeadService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<ITransactionHeadListResponse>) => {
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

  searchSubject = new Subject<string>();
  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }

  onSortring(event: any) {
    this.pagination.orderBy = event.sorts[0].prop;
    this.pagination.isAscending = event.sorts[0].dir === 'asc';
    this.fetchData();
  }

  addRow() {
    const modalRef = this.modalService.open(
      AddTransactionHeadComponent,
      ModalOption.lg
    );
    modalRef.componentInstance.isEditing = false;
    modalRef.result
      .then((response: any) => {
        if (response.success) {
          this.fetchData();
        }
      })
      .catch(() => {});
  }

  editRow(row: ITransactionHeadListResponse) {
    const modalRef = this.modalService.open(
      AddTransactionHeadComponent,
      ModalOption.lg
    );
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;
    modalRef.result
      .then((response: any) => {
        if (response.success) {
          this.fetchData();
        }
      })
      .catch(() => {});
  }

  deleteRow(row: ITransactionHeadListResponse) {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.transactionHeadService.remove(row.id).subscribe({
          next: (response) => {
            if (response) {
              this.removeRecord(row);
            }
          },
          error: (err: ErrorResponse) => {
            this.toastr.error(formatErrorMessage(err));
          },
        });
      }
    });
  }

  private removeRecord(row: ITransactionHeadListResponse) {
    this.data = this.data.filter((x) => x.id !== row.id);
  }

  private showSuccess(message: string) {
    this.toastr.success(message, 'Success');
  }

  private deleteRecordSuccess(count: number) {
    const msg = count === 1 ? MessageHub.DELETE_ONE : MessageHub.DELETE_BATCH;
    this.toastr.success(msg, 'Success');
  }
}
