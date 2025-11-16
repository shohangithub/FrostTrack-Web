import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormControl,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { SwalConfirm } from 'app/theme-config';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { SaleReturnService } from 'app/sales/services/sale-return.service';
import { MessageHub } from '../../../config/message-hub';
import { ISaleReturnListResponse } from 'app/sales/models/sale-return.interface';
import { PaginationResult } from '../../../core/models/pagination-result';
import { PaginationQuery } from '../../../core/models/pagination-query';
import { COMMON_STATUS_LIST } from '../../../common/data/settings-data';

@Component({
  selector: 'app-sale-return-record',
  templateUrl: './sale-return-record.component.html',
  styleUrls: ['./sale-return-record.component.sass'],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
})
export class SaleReturnRecordComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  data: ISaleReturnListResponse[] = [];
  paging: any = {};
  pagination: PaginationQuery = {
    pageIndex: 0,
    pageSize: 10,
    orderBy: '',
    isAscending: true,
  };
  searchTerm = '';

  loadingIndicator = true;
  reorderable = true;
  register!: UntypedFormGroup;
  editForm!: UntypedFormGroup;
  isEditing = false;
  isSubmitted = false;
  selectedRowData: any = {};
  columns = [
    { name: 'Return Number', prop: 'returnNumber' },
    { name: 'Return Date', prop: 'returnDate' },
    { name: 'Customer', prop: 'customer.customerName' },
    { name: 'Return Amount', prop: 'returnAmount' },
    { name: 'Reason', prop: 'reason' },
    { name: 'Actions' },
  ];
  statusList = COMMON_STATUS_LIST;
  MessageHub = MessageHub;

  constructor(
    private fb: UntypedFormBuilder,
    private toastr: ToastrService,
    private saleReturnService: SaleReturnService,
    private modalService: NgbModal,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initFormData();
    this.fetchData();
    this.initSearchSubject();
  }

  initFormData() {
    this.register = this.fb.group({
      returnNumber: ['', [Validators.required]],
      returnDate: ['', [Validators.required]],
      reason: ['', [Validators.required]],
    });

    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      returnNumber: new UntypedFormControl(),
      returnDate: new UntypedFormControl(),
      reason: new UntypedFormControl(),
    });
  }

  fetchData() {
    this.loadingIndicator = true;
    this.saleReturnService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<ISaleReturnListResponse>) => {
        this.data = response.data;
        this.paging = response.paging;
        this.loadingIndicator = false;
      },
      error: (err: ErrorResponse) => {
        this.loadingIndicator = false;
        const errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  setPage(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset;
    this.fetchData();
  }

  editRow(row: any) {
    this.router.navigate(['/sales/sale-return/edit', row.id]);
  }

  removeRecord(row: any) {
    this.data = this.data.filter((value) => {
      return value.id !== row.id;
    });
  }

  delete(row: any) {
    Swal.fire({
      title: this.MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.value) {
        this.saleReturnService.remove(row.id).subscribe({
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

  selected: any[] = [];
  onSelect({ selected }: any) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);
  }

  deleteSelectedRows() {
    if (this.selected.length > 0) {
      Swal.fire({
        title: this.MessageHub.DELETE_CONFIRM,
        showCancelButton: true,
        confirmButtonColor: SwalConfirm.confirmButtonColor,
        cancelButtonColor: SwalConfirm.cancelButtonColor,
        confirmButtonText: 'Yes',
      }).then((result) => {
        if (result.value) {
          const ids = this.selected.map((row) => row.id);
          this.saleReturnService.batchDelete(ids).subscribe({
            next: (response) => {
              if (response) {
                this.selected.forEach((selectedRow) => {
                  this.removeRecord(selectedRow);
                });
                this.selected = [];
              }
            },
            error: (err: ErrorResponse) => {
              this.toastr.error(formatErrorMessage(err));
            },
          });
        }
      });
    } else {
      this.toastr.warning('Please select rows to delete');
    }
  }

  searchSubject = new Subject<any>();

  initSearchSubject() {
    this.searchSubject
      .pipe(debounceTime(500), distinctUntilChanged())
      .subscribe((searchValue) => {
        this.searchTerm = searchValue;
        this.pagination.pageIndex = 0;
        this.fetchData();
      });
  }

  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }

  viewDetails(row: any) {
    // Navigate to view details page or open modal
    this.router.navigate(['/sales/sale-return/view', row.id]);
  }

  createNew() {
    this.router.navigate(['/sales/sale-return/create']);
  }
}
