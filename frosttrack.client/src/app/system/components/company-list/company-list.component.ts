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
import { ToastrService, ToastrModule } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { PagingResponse } from '../../../core/models/pagination-result';
import { PaginationQuery } from '../../../core/models/pagination-query';
import { DefaultPagination } from '../../../config/pagination';
import { ModalOption } from '@config/modal-option';
import { SwalConfirm } from 'app/theme-config';
import {
  ErrorResponse,
  formatErrorMessage,
} from '../../../utils/server-error-handler';
import { CompanyService } from '../../services/company.service';
import { ICompanyListResponse } from '../../models/company.interface';
import { CompanyConfigComponent } from '../company-config/company-config.component';

@Component({
  selector: 'app-company-list',
  templateUrl: './company-list.component.html',
  styleUrls: ['./company-list.component.scss'],
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
  ],
  providers: [CompanyService],
})
export class CompanyListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  data: ICompanyListResponse[] = [];
  filteredData: ICompanyListResponse[] = [];
  register!: UntypedFormGroup;
  loadingIndicator = true;
  scrollBarHorizontal = window.innerWidth < 1200;
  reorderable = true;
  selected: ICompanyListResponse[] = [];

  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: 'name',
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;

  selection!: SelectionType;
  searchSubject = new Subject<string>();

  constructor(
    private modalService: NgbModal,
    private toastr: ToastrService,
    private companyService: CompanyService
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loadingIndicator = true;
    this.companyService.getList().subscribe({
      next: (response: ICompanyListResponse[]) => {
        this.data = response;
        this.filteredData = response;
        this.loadingIndicator = false;
      },
      error: (err: ErrorResponse) => {
        this.loadingIndicator = false;
        const errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  addNew() {
    const modalRef = this.modalService.open(CompanyConfigComponent, {
      ...ModalOption,
      size: 'lg',
    });
    modalRef.componentInstance.isEditing = false;

    modalRef.result.then(
      (result) => {
        if (result) {
          this.loadData();
        }
      },
      () => {}
    );
  }

  editCall(row: ICompanyListResponse) {
    const modalRef = this.modalService.open(CompanyConfigComponent, {
      ...ModalOption,
      size: 'lg',
    });
    modalRef.componentInstance.isEditing = true;
    modalRef.componentInstance.row = row;

    modalRef.result.then(
      (result) => {
        if (result) {
          this.loadData();
        }
      },
      () => {}
    );
  }

  deleteItem(row: ICompanyListResponse) {
    Swal.fire(SwalConfirm).then((result) => {
      if (result.isConfirmed) {
        this.companyService.deleteCompany(row.id).subscribe({
          next: (response: boolean) => {
            if (response) {
              this.loadData();
            }
          },
          error: (err: ErrorResponse) => {
            const errString = formatErrorMessage(err);
            this.toastr.error(errString);
          },
        });
      }
    });
  }

  filterDatatable(event: any) {
    const val = event.target.value.toLowerCase();
    const filteredData = this.data.filter((d: ICompanyListResponse) => {
      return (
        d.name.toLowerCase().indexOf(val) !== -1 ||
        d.businessCurrency.toLowerCase().indexOf(val) !== -1 ||
        d.codeGenerationName.toLowerCase().indexOf(val) !== -1 ||
        !val
      );
    });
    this.filteredData = filteredData;
  }

  onSelect({ selected }: any) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);
  }
}
