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
import {
  IAssetListResponse,
  IAssetPaginationQuery,
  IAssetResponse,
} from '../../models/asset.interface';
import { AssetService } from '../../services/asset.service';
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
import { AddAssetComponent } from './add-asset/add-asset.component';
import { ModalOption } from '@config/modal-option';

@Component({
  selector: 'app-asset',
  templateUrl: './asset.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [AssetService],
})
export class AssetComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  selectedRowData!: IAssetResponse;
  data: IAssetListResponse[] = [];
  filteredData: any[] = [];
  register!: UntypedFormGroup;
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IAssetListResponse[] = [];
  pagination: IAssetPaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
    status: 'active',
  };
  activeStatus: string = 'active';
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
    private assetService: AssetService,
    private layoutService: LayoutService,
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
    this.assetService.getWithPaginationStatus(this.pagination).subscribe({
      next: (response: PaginationResult<IAssetListResponse>) => {
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

  // filter data table
  private searchSubject = new Subject<boolean>();

  filterDatatable(event: any): void {
    // this.pagination.openText = event.target.value?.length > 0;
    // this.searchSubject.next(this.pagination.openText);
  }

  addRow() {
    const modalRef = this.modalService.open(AddAssetComponent, ModalOption.lg);
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

  editRow(row: IAssetListResponse) {
    const modalRef = this.modalService.open(AddAssetComponent, ModalOption.lg);
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;
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

  delete(row: IAssetListResponse) {
    Swal.fire({
      title: 'Move to trash?',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes',
    }).then((result) => {
      if (result.isConfirmed) {
        this.assetService.softDelete(row.id).subscribe({
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
      this.toastr.warning('Please select at least one asset to delete.');
      return;
    }

    Swal.fire({
      ...SwalConfirm,
      text: `Are you sure you want to delete ${this.selected.length} selected asset(s)?`,
    }).then((result) => {
      if (result.isConfirmed) {
        const ids = this.selected.map((asset) => asset.id);
        this.assetService.batchDelete(ids).subscribe({
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

  setStatus(status: string) {
    this.activeStatus = status;
    this.pagination.status = status as any;
    this.pagination.pageIndex = 0;
    this.fetchData();
  }

  restore(row: any) {
    this.assetService
      .restore(row.id)
      .subscribe({ next: () => this.fetchData() });
  }

  archive(row: any) {
    this.assetService
      .archive(row.id)
      .subscribe({ next: () => this.fetchData() });
  }

  unarchive(row: any) {
    this.assetService
      .unarchive(row.id)
      .subscribe({ next: () => this.fetchData() });
  }

  permanentDelete(row: any) {
    Swal.fire({
      title: 'Permanently delete?',
      text: 'This cannot be undone!',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#dc3545',
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Delete',
    }).then((result) => {
      if (result.value) {
        this.assetService.permanentDelete(row.id).subscribe({
          next: () => {
            this.fetchData();
          },
          error: () => {},
        });
      }
    });
  }
}
