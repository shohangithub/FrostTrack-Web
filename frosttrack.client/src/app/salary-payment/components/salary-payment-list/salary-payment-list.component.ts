import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Configuration } from '@config/configuration';
import { DefaultPagination } from '@config/pagination';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import {
  DatatableComponent,
  SelectionType,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import { ISalaryPaymentList } from 'app/salary-payment/models/salary-payment.interface';
import { SalaryPaymentService } from 'app/salary-payment/services/salary-payment.service';
import { ROLES } from 'app/common/data/settings-data';
import { formatErrorMessage } from 'app/utils/server-error-handler';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-salary-payment-list',
  templateUrl: './salary-payment-list.component.html',
  standalone: true,
  imports: [NgxDatatableModule, DatePipe, DecimalPipe, CommonModule],
})
export class SalaryPaymentListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  scrollBarHorizontal = window.innerWidth < 1200;
  data: ISalaryPaymentList[] = [];
  filteredData: ISalaryPaymentList[] = [];
  loadingIndicator = true;
  isRowSelected = false;
  reorderable = true;
  selected: ISalaryPaymentList[] = [];
  pageSize = DefaultPagination.PAGESIZE;
  canEdit: boolean = false;
  canDelete: boolean = false;
  searchSubject = new Subject<string>();
  selection = SelectionType.checkbox;
  searchText: string = '';

  constructor(
    private router: Router,
    private toastr: ToastrService,
    private authService: AuthService,
    private layoutService: LayoutService,
    private salaryPaymentService: SalaryPaymentService
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
      .subscribe((value: string) => {
        this.searchText = value.toLowerCase();
        this.applyFilter();
      });
  }

  fetchData() {
    this.loadingIndicator = true;

    this.salaryPaymentService.getSalaryPaymentList().subscribe({
      next: (result: ISalaryPaymentList[]) => {
        this.data = result;
        this.applyFilter();
        this.loadingIndicator = false;
      },
      error: (err: any) => {
        this.loadingIndicator = false;
        const errorMessage = formatErrorMessage(err);
        this.toastr.error(errorMessage, 'Load Error');
      },
    });
  }

  applyFilter() {
    if (!this.searchText) {
      this.filteredData = [...this.data];
    } else {
      this.filteredData = this.data.filter(
        (item) =>
          item.employeeName.toLowerCase().includes(this.searchText) ||
          item.employeeCode.toLowerCase().includes(this.searchText) ||
          item.period.toLowerCase().includes(this.searchText) ||
          item.paymentMethod.toLowerCase().includes(this.searchText)
      );
    }
  }

  onSearch(event: any) {
    this.searchSubject.next(event.target.value);
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

  exportToExcel() {
    // Export functionality can be added later
    this.toastr.info('Export functionality coming soon');
  }
}
