import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import {
  IEmployeeListResponse,
  IEmployeeResponse,
} from '../../models/employee.interface';
import { EmployeeService } from '../../services/employee.service';
import { CommonModule } from '@angular/common';
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
import { AddEmployeeComponent } from './add-employee/add-employee.component';
import { ModalOption } from '../../../config/modal-option';
import { Configuration } from '../../../config/configuration';
import { MessageHub } from '@config/message-hub';

@Component({
  selector: 'app-employee',
  templateUrl: './employee.component.html',
  styleUrls: [],
  standalone: true,
  imports: [NgxDatatableModule, ToastrModule, CommonModule],
  providers: [EmployeeService],
})
export class EmployeeComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  selectedRowData!: IEmployeeResponse;
  data: IEmployeeListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IEmployeeListResponse[] = [];
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;

  statusList = COMMON_STATUS_LIST;

  @ViewChild(DatatableComponent, { static: false }) table2!: DatatableComponent;
  selection!: SelectionType;

  constructor(
    private modalService: NgbModal,
    private toastr: ToastrService,
    private employeeService: EmployeeService,
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

  ngOnInit(): void {
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
    this.employeeService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IEmployeeListResponse>) => {
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

  setPage(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset + 1;
    this.fetchData();
  }

  onSort(event: any) {
    this.pagination.orderBy = event.column.prop;
    this.pagination.isAscending = event.newValue === 'asc';
    this.fetchData();
  }

  // add new record
  addRow() {
    const modalRef = this.modalService.open(
      AddEmployeeComponent,
      ModalOption.xl
    );
    modalRef.componentInstance.isEditing = false;
    modalRef.result
      .then((result) => {
        if (result?.success) {
          this.fetchData();
        }
      })
      .catch(() => {
        // Modal dismissed
      });
  }

  // edit record
  editRow(row: any) {
    const modalRef = this.modalService.open(
      AddEmployeeComponent,
      ModalOption.xl
    );
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;
    modalRef.result.then((response) => {
      if (response?.success) {
        this.fetchData();
      }
    });
  }

  // delete record
  delete(row: any) {
    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.isConfirmed) {
        this.employeeService.remove(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {},
        });
      }
    });
  }

  deleteSelected() {
    if (this.selected.length === 0) {
      this.toastr.warning('Please select at least one employee to delete.');
      return;
    }

    Swal.fire({
      ...SwalConfirm,
      text: `Are you sure you want to delete ${this.selected.length} selected employee(s)?`,
    }).then((result) => {
      if (result.isConfirmed) {
        const ids = this.selected.map((employee) => employee.id);
        this.employeeService.batchDelete(ids).subscribe({
          next: () => {
            this.selected = [];
            this.isRowSelected = false;
            this.fetchData();
          },
          error: () => {},
        });
      }
    });
  }

  changePagination(event: any) {
    this.pagination.pageIndex = event.offset + 1;
    this.pagination.pageSize = event.limit;
    this.fetchData();
  }

  onSortring(event: any) {
    this.pagination.orderBy = event.sorts[0].prop;
    this.pagination.isAscending = event.sorts[0].dir === 'asc';
    this.fetchData();
  }

  searchSubject = new Subject<any>();
  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }
}
