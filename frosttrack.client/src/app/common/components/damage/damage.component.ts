import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  SortType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { IDamageListResponse } from '../../models/damage.interface';
import { DamageService } from '../../services/damage.service';
import { CommonModule } from '@angular/common';
import { SwalConfirm } from 'app/theme-config';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  PaginationResult,
  PagingResponse,
} from '../../../core/models/pagination-result';
import { AddDamageComponent } from './add-damage/add-damage.component';
import { PaginationQuery } from '../../../core/models/pagination-query';
import { ModalOption } from '../../../config/modal-option';
import { MessageHub } from '../../../config/message-hub';
import { DefaultPagination } from '../../../config/pagination';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-damage',
  templateUrl: './damage.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [DamageService],
})
export class DamageComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  data: IDamageListResponse[] = [];
  filteredData: any[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IDamageListResponse[] = [];
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;
  @ViewChild(DatatableComponent, { static: false }) table2!: DatatableComponent;
  selection!: SelectionType;
  sortType = SortType.single;

  constructor(
    private modalService: NgbModal,
    private toastr: ToastrService,
    private damageService: DamageService,
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
        this.damageService.batchDelete(ids).subscribe({
          next: () => {
            this.selected.forEach((row) => {
              this.removeRecord(row);
            });
            this.selected = [];
            this.isRowSelected = false;
          },
          error: () => {},
        });
      }
    });
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
        this.damageService.remove(row.id).subscribe({
          next: () => {
            this.removeRecord(row);
          },
          error: () => {
            // Error is handled by service
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

  add() {
    this.addRow();
  }

  // add new record
  addRow() {
    const modalRef = this.modalService.open(AddDamageComponent, ModalOption.lg);
    modalRef.result.then((response) => {
      if (response?.success) {
        this.fetchData();
      }
    });
  }

  edit(row: any) {
    this.editRow(row);
  }

  // edit record
  editRow(row: any) {
    const modalRef = this.modalService.open(AddDamageComponent, ModalOption.lg);
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;
    modalRef.result.then((response) => {
      if (response?.success) {
        const result = response.data;
        this.data = this.data.map((value) => {
          if (value.id === result.id) {
            return {
              ...value,
              damageNumber: result.damageNumber,
              damageDate: result.damageDate,
              productName: result.productName,
              unitName: result.unitName,
              quantity: result.quantity,
              unitCost: result.unitCost,
              totalCost: result.totalCost,
              reason: result.reason,
              status: result.status,
            };
          }
          return value;
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
    this.damageService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IDamageListResponse>) => {
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

  searchSubject = new Subject<any>();
  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    this.searchSubject.next(val);
  }
}
