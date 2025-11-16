import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  UntypedFormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { RouterLink } from '@angular/router';
import {
  IUnitConversionListResponse,
  IUnitConversionRequest,
  IUnitConversionResponse,
} from '../../models/unit-conversion.interface';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { SwalConfirm, ThemeConfig } from 'app/theme-config';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  PaginationResult,
  PagingResponse,
} from '../../../core/models/pagination-result';
import { PaginationQuery } from '../../../core/models/pagination-query';
import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { UnitConversionService } from 'app/common/services/unit-conversion.service';
import { BaseUnitService } from '../../services/base-unit.service';
import { ILookup } from '../../../core/models/lookup';
import { DefaultPagination } from '../../../config/pagination';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-unit-conversion',
  templateUrl: './unit-conversion.component.html',
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
  providers: [UnitConversionService, BaseUnitService],
})
export class UnitConversionComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;
  rows = [];
  scrollBarHorizontal = window.innerWidth < 1200;
  selectedRowData!: IUnitConversionResponse;
  data: IUnitConversionListResponse[] = [];
  filteredData: any[] = [];
  baseUnits: ILookup<number>[] = [];
  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  loadingIndicator = true;
  isRowSelected = false;
  selectedOption!: string;
  reorderable = true;
  selected: IUnitConversionListResponse[] = [];
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
  constructor(
    private fb: UntypedFormBuilder,
    private modalService: NgbModal,
    private toastr: ToastrService,
    private unitConversionService: UnitConversionService,
    private baseUnitService: BaseUnitService,
    private layoutService: LayoutService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      unitName: new UntypedFormControl(),
      baseUnitId: new UntypedFormControl(),
      conversionValue: new UntypedFormControl(),
      description: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
    });

    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.selection = SelectionType.single;
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
  checkSelectable($event: IUnitConversionListResponse) {
    return $event.conversionValue !== 1;
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
        this.unitConversionService.batchDelete(ids).subscribe({
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
    this.fetchBaseUnitLookup();

    //subject call change open text search
    this.searchSubject
      .pipe(debounceTime(1000), distinctUntilChanged())
      .subscribe((value: any) => {
        this.pagination.openText = value;
        this.fetchData();
      });

    this.register = this.fb.group({
      unitName: ['', [Validators.required]],
      baseUnitId: ['', [Validators.required]],
      conversionValue: ['', [Validators.required]],
      description: [''],
      isActive: ['', [Validators.required]],
    });
  }

  fetchData() {
    this.unitConversionService.getWithPagination(this.pagination).subscribe({
      next: (response: PaginationResult<IUnitConversionListResponse>) => {
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

  fetchBaseUnitLookup() {
    this.baseUnitService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.baseUnits = response;
        this.loadingIndicator = false;
      },
      error: (err: ErrorResponse) => {
        this.loadingIndicator = false;
        var errString = formatErrorMessage(err);
        this.toastr.error(errString);
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
  addRow(content: any) {
    this.modalService.open(content, {
      ariaLabelledBy: 'modal-basic-title',
      size: 'lg',
    });
  }
  // edit record
  editRow(row: any, rowIndex: number, content: any) {
    this.modalService.open(content, {
      ariaLabelledBy: 'modal-basic-title',
      size: 'lg',
    });

    this.unitConversionService.getById(row.id).subscribe({
      next: (response: IUnitConversionResponse) => {
        this.editForm.setValue({
          id: response.id,
          unitName: response.unitName,
          baseUnitId: response.baseUnitId,
          conversionValue: response.conversionValue,
          description: response.description,
          isActive: response.isActive || false,
        });
        this.loadingIndicator = false;
      },
      error: (err) => {
        console.log(err);
        this.loadingIndicator = false;
      },
    });

    this.selectedRowData = row;
  }
  // save add new record
  save(form: UntypedFormGroup) {
    if (this.register.valid) {
      const payload: IUnitConversionRequest = { ...form.value };
      this.unitConversionService.create(payload).subscribe({
        next: (response: IUnitConversionResponse) => {
          this.fetchData();
          form.reset();
          this.modalService.dismissAll();
        },
        error: () => {
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }
  // save record on edit
  edit(form: UntypedFormGroup) {
    if (this.editForm.valid) {
      const formData = form.value;
      const payload: IUnitConversionRequest = { ...formData };
      this.unitConversionService.update(formData.id, payload).subscribe({
        next: (response: IUnitConversionResponse) => {
          this.data = this.data.filter((value, key) => {
            if (value.id == response.id) {
              value.unitName = response.unitName;
              value.baseUnitId = response.baseUnitId;
              value.conversionValue = response.conversionValue;
              value.description = response.description;
              value.status = response.status;
            }
            this.modalService.dismissAll();
            return true;
          });
          form.reset();
          this.modalService.dismissAll();
        },
        error: () => {
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
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
        this.unitConversionService.remove(row.id).subscribe({
          next: (response) => {
            if (response) {
              this.removeRecord(row);
              this.deleteRecordSuccess(1);
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

  // get random id
  getId(min: number, max: number) {
    // min and max included
    return Math.floor(Math.random() * (max - min + 1) + min);
  }

  addRecordSuccess() {
    // Success message is now handled by the service
  }
  updateRecordSuccess() {
    // Success message is now handled by the service
  }

  deleteRecordSuccess(count: number) {
    // Success message is now handled by the service
  }
}
