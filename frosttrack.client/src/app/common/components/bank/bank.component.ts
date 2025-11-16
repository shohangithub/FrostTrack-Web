import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  UntypedFormGroup,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { IBankListResponse, IBankResponse } from '../../models/bank.interface';
import { BankService } from '../../services/bank.service';
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
import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { DefaultPagination } from '../../../config/pagination';
import { LayoutService } from '@core/service/layout.service';
import { AddBankComponent } from './add-bank/add-bank.component';
import { ModalOption } from '@config/modal-option';

@Component({
  selector: 'app-bank',
  templateUrl: './bank.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [BankService],
})
export class BankComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  selectedRowData!: IBankResponse;
  data: IBankListResponse[] = [];
  filteredData: any[] = [];
  register!: UntypedFormGroup;
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IBankListResponse[] = [];
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;

  statusList = COMMON_STATUS_LIST;
  MessageHub = {
    ADD: 'Add Record Successfully',
    UPDATE: '',
    DELETE_CONFIRM: 'Are you sure?',
    DELETE: '',
  };

  @ViewChild(DatatableComponent, { static: false }) table2!: DatatableComponent;
  selection!: SelectionType;
  isGeneratingCode: boolean = false;

  constructor(
    private modalService: NgbModal,
    private toastr: ToastrService,
    private bankService: BankService,
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
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        const ids = this.selected.map((x) => x.id);
        this.bankService.batchDelete(ids).subscribe({
          next: () => {
            this.selected.forEach((row) => {
              this.removeRecord(row);
            });
            this.deleteRecordSuccess();
            this.selected = [];
            this.isRowSelected = false;
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
      .pipe(debounceTime(1000), distinctUntilChanged())
      .subscribe((value: any) => {
        this.pagination.openText = value;
        this.fetchData();
      });
  }

  fetchData() {
    this.bankService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IBankListResponse>) => {
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
    const modalRef = this.modalService.open(AddBankComponent, ModalOption.lg);
    modalRef.result.then((response) => {
      if (response?.success) {
        this.fetchData();
      }
    });
  }
  // edit record
  editRow(row: any) {
    const modalRef = this.modalService.open(AddBankComponent, ModalOption.lg);
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;
    modalRef.result.then((response) => {
      if (response?.success) {
        const result = response.data;
        this.data = this.data.filter((value) => {
          if (value.id == result.id) {
            value.bankName = result.bankName;
            value.bankCode = result.bankCode;
            value.bankBranch = result.bankBranch;
            value.accountNumber = result.accountNumber;
            value.accountTitle = result.accountTitle;
            value.swiftCode = result.swiftCode;
            value.routingNumber = result.routingNumber;
            value.ibanNumber = result.ibanNumber;
            value.contactPerson = result.contactPerson;
            value.contactPhone = result.contactPhone;
            value.contactEmail = result.contactEmail;
            value.address = result.address;
            value.openingBalance = result.openingBalance;
            value.currentBalance = result.currentBalance;
            value.description = result.description;
            value.isMainAccount = result.isMainAccount;
            value.status = result.status;
          }
          return true;
        });
      }
    });
  }
  // delete single row
  delete(row: any) {
    Swal.fire({
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.bankService.remove(row.id).subscribe({
          next: () => {
            this.removeRecord(row);
            this.deleteRecordSuccess();
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

  deleteRecordSuccess() {
    // Success message is now handled by the service
  }
}
