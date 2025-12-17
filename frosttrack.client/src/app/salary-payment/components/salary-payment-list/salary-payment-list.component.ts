import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Configuration } from '@config/configuration';
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
import { ISalaryPaymentList } from 'app/salary-payment/models/salary-payment.interface';
import { SalaryPaymentService } from 'app/salary-payment/services/salary-payment.service';
import { EmployeeService } from 'app/common/services/employee.service';
import { ROLES } from 'app/common/data/settings-data';
import { formatErrorMessage } from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import Swal from 'sweetalert2';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ILookup } from '@core/models/lookup';
import { MessageHub } from '@config/message-hub';
import { SwalConfirm } from 'app/theme-config';

@Component({
  selector: 'app-salary-payment-list',
  templateUrl: './salary-payment-list.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    DatePipe,
    DecimalPipe,
    CommonModule,
    NgSelectModule,
    ReactiveFormsModule,
  ],
})
export class SalaryPaymentListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  scrollBarHorizontal = window.innerWidth < 1200;
  data: ISalaryPaymentList[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  reorderable = true;
  selected: ISalaryPaymentList[] = [];
  pagination: PaginationQuery = {
    pageSize: DefaultPagination.PAGESIZE,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };
  paging: PagingResponse | undefined;
  canEdit: boolean = false;
  canDelete: boolean = false;
  searchSubject = new Subject<string>();
  selection = SelectionType.checkbox;

  // Filter properties
  filterForm: UntypedFormGroup;
  employeeList: ILookup<number>[] = [];
  isEmployeeLoading = false;
  months: ILookup<number>[] = [
    { value: 0, text: 'All Months' },
    { value: 1, text: 'January' },
    { value: 2, text: 'February' },
    { value: 3, text: 'March' },
    { value: 4, text: 'April' },
    { value: 5, text: 'May' },
    { value: 6, text: 'June' },
    { value: 7, text: 'July' },
    { value: 8, text: 'August' },
    { value: 9, text: 'September' },
    { value: 10, text: 'October' },
    { value: 11, text: 'November' },
    { value: 12, text: 'December' },
  ];
  years: ILookup<number>[] = [];

  constructor(
    private router: Router,
    private toastr: ToastrService,
    private authService: AuthService,
    private layoutService: LayoutService,
    private salaryPaymentService: SalaryPaymentService,
    private employeeService: EmployeeService,
    private fb: UntypedFormBuilder
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.layoutService.loadCurrentRoute();
    this.setPermissions();
    this.initializeYears();

    this.filterForm = this.fb.group({
      employeeId: [null],
      month: [null],
      year: [null],
    });
  }

  private setPermissions() {
    const roles = this.authService.getUserRoles();
    if (roles.includes(ROLES.SUPERADMIN) || roles.includes(ROLES.ADMIN)) {
      this.canEdit = true;
      this.canDelete = true;
    }
  }

  private initializeYears() {
    const currentYear = new Date().getFullYear();
    this.years.push({ value: 0, text: 'All Years' });
    for (let i = currentYear; i >= currentYear - 5; i--) {
      this.years.push({ value: i, text: i.toString() });
    }
  }

  onSelect({ selected }: { selected: any }) {
    this.selected.splice(0, this.selected.length);
    this.selected.push(...selected);
    this.isRowSelected = this.selected.length > 0;
  }

  ngOnInit() {
    this.loadEmployees();
    this.fetchData();

    this.searchSubject
      .pipe(
        debounceTime(Configuration.SEARCH_DEBOUNCE_TIME),
        distinctUntilChanged()
      )
      .subscribe((value: string) => {
        this.pagination.openText = value as any;
        this.pagination.pageIndex = DefaultPagination.PAGEINDEX;
        this.fetchData();
      });
  }

  loadEmployees() {
    this.isEmployeeLoading = true;
    this.employeeService.getLookup().subscribe({
      next: (result: ILookup<number>[]) => {
        this.employeeList = [{ value: 0, text: 'All Employees' }, ...result];
        this.isEmployeeLoading = false;
      },
      error: (err: any) => {
        console.error('Error loading employees', err);
        this.isEmployeeLoading = false;
      },
    });
  }

  fetchData() {
    this.loadingIndicator = true;

    const employeeId = this.filterForm.get('employeeId')?.value;
    const month = this.filterForm.get('month')?.value;
    const year = this.filterForm.get('year')?.value;

    this.salaryPaymentService
      .getWithPagination(
        this.pagination,
        employeeId && employeeId > 0 ? employeeId : undefined,
        month && month > 0 ? month : undefined,
        year && year > 0 ? year : undefined
      )
      .subscribe({
        next: (result: PaginationResult<ISalaryPaymentList>) => {
          this.data = result.data;
          this.paging = result.paging;
          this.loadingIndicator = false;
        },
        error: (err: any) => {
          this.loadingIndicator = false;
          const errorMessage = formatErrorMessage(err);
          this.toastr.error(errorMessage, 'Load Error');
        },
      });
  }

  onSearch(event: any) {
    this.searchSubject.next(event.target.value);
  }

  onFilterChange() {
    this.pagination.pageIndex = DefaultPagination.PAGEINDEX;
    this.fetchData();
  }

  clearFilters() {
    this.filterForm.reset({
      employeeId: null,
      month: null,
      year: null,
    });
    this.pagination.openText = undefined;
    this.pagination.pageIndex = DefaultPagination.PAGEINDEX;
    this.fetchData();
  }

  setPage(pageInfo: any) {
    this.pagination.pageIndex = pageInfo.offset;
    this.fetchData();
  }

  onSort(event: any) {
    this.pagination.orderBy = event.sorts[0].prop;
    this.pagination.isAscending = event.sorts[0].dir === 'asc';
    this.fetchData();
  }

  add() {
    this.router.navigate(['/salary-payment/add']);
  }

  edit(id: number) {
    this.router.navigate(['/salary-payment/edit', id]);
  }

  view(id: number) {
    this.router.navigate(['/salary-payment/view', id]);
  }

  printReceipt(row: ISalaryPaymentList) {
    this.router.navigate(['/salary-payment/receipt-print', row.id, 'list']);
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

  getMonthName(period: string): string {
    // Period format is "MM/YYYY"
    const parts = period.split('/');
    if (parts.length !== 2) return period;

    const monthNum = parseInt(parts[0], 10);
    const year = parts[1];
    const monthName =
      this.months.find((m) => m.value === monthNum)?.text || parts[0];

    return `${monthName} ${year}`;
  }

  canDeletePayment(createdAt: Date): boolean {
    if (!this.canDelete) return false;

    const createdDate = new Date(createdAt);
    const now = new Date();
    const oneDayInMs = 24 * 60 * 60 * 1000;
    const timeDiff = now.getTime() - createdDate.getTime();

    return timeDiff <= oneDayInMs;
  }

  delete(row: ISalaryPaymentList) {
    if (!this.canDeletePayment(row.createdAt)) {
      this.toastr.error(
        'Cannot delete salary payment. Deletion is only allowed within one day of creation.',
        'Delete Not Allowed'
      );
      return;
    }

    Swal.fire({
      title: MessageHub.DELETE_CONFIRM,
      text: `Are you sure you want to delete the salary payment for ${row.employeeName}?`,
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes, delete it!',
    }).then((result) => {
      if (result.value) {
        this.salaryPaymentService
          .deleteSalaryPayment(row.id.toString())
          .subscribe({
            next: () => {
              this.toastr.success(
                'Salary payment deleted successfully',
                'Deleted'
              );
              this.fetchData();
            },
            error: (err: any) => {
              const errorMessage = formatErrorMessage(err);
              this.toastr.error(errorMessage, 'Delete Error');
            },
          });
      }
    });
  }

  exportToExcel() {
    // Export functionality can be added later
    this.toastr.info('Export functionality coming soon');
  }
}
