import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  NgxDatatableModule,
  SortType,
} from '@swimlane/ngx-datatable';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PaginatedComponent } from '../../../core/base/paginated-component';
import { SupplierPaymentService } from '../../services/supplier-payment.service';
import { ISupplierPaymentListResponse } from '../../models/supplier-payment.interface';
import { PaymentMethodService } from '../../../common/services/payment-method.service';

@Component({
  selector: 'app-supplier-payment-record',
  templateUrl: './supplier-payment-record.component.html',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NgxDatatableModule],
})
export class SupplierPaymentRecordComponent
  extends PaginatedComponent
  implements OnInit
{
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  rows: ISupplierPaymentListResponse[] = [];
  reorderable = true;
  sortType: SortType = SortType.single;

  // Filters
  paymentTypeFilter = '';
  paymentMethodFilter = '';
  dateFromFilter = '';
  dateToFilter = '';

  paymentTypes = [
    { value: '', text: 'All Types' },
    { value: 'Supplier', text: 'Supplier Payment' },
    { value: 'Customer', text: 'Customer Payment' },
  ];

  paymentMethods: {
    value: string;
    text: string;
    icon?: string;
    colorClass?: string;
    description?: string;
  }[] = [{ value: '', text: 'All Methods' }];

  constructor(
    private supplierPaymentService: SupplierPaymentService,
    private paymentMethodService: PaymentMethodService
  ) {
    super();
    this.pageSize = 20; // Override default page size
  }

  ngOnInit(): void {
    this.loadPaymentMethods();
    this.loadData();
  }

  loadPaymentMethods(): void {
    this.paymentMethodService
      .getActiveList()
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response && Array.isArray(response)) {
            const methods = response.map((pm: any) => {
              // Use iconClass from API if available, otherwise fallback to category-based mapping
              let icon = pm.iconClass || 'fa fa-money-bill-wave';
              let colorClass = 'text-success';
              const description = pm.description || 'Payment method';

              // Determine color class based on category
              switch (pm.category?.toLowerCase()) {
                case 'cash':
                  colorClass = 'text-success';
                  if (!pm.iconClass) icon = 'fa fa-money-bill-wave';
                  break;
                case 'bank':
                  colorClass = 'text-primary';
                  if (!pm.iconClass) icon = 'fa fa-university';
                  break;
                case 'card':
                  colorClass = 'text-warning';
                  if (!pm.iconClass) icon = 'fa fa-credit-card';
                  break;
                case 'digital':
                  colorClass = 'text-info';
                  if (!pm.iconClass) icon = 'fa fa-mobile-alt';
                  break;
                default:
                  colorClass = 'text-secondary';
                  if (!pm.iconClass) icon = 'fa fa-money-bill-wave';
                  break;
              }

              return {
                value: pm.methodName,
                text: pm.methodName,
                icon: icon,
                colorClass: colorClass,
                description: description,
              };
            });
            this.paymentMethods = [
              { value: '', text: 'All Methods' },
              ...methods,
            ];
          }
        },
      });
  }

  loadData(): void {
    const additionalFilters = {
      paymentType: this.paymentTypeFilter,
      paymentMethod: this.paymentMethodFilter,
      dateFrom: this.dateFromFilter,
      dateTo: this.dateToFilter,
    };

    this.loadPaginatedData(
      (query) => this.supplierPaymentService.getWithPagination(query),
      {
        additionalFilters,
        onDataLoaded: (data) => {
          this.rows = data;
        },
      }
    );
  }

  override clearFilters(): void {
    super.clearFilters();
    this.paymentTypeFilter = '';
    this.paymentMethodFilter = '';
    this.dateFromFilter = '';
    this.dateToFilter = '';
    this.loadData();
  }

  exportToExcel(): void {
    // TODO: Implement export functionality
    console.log('Export functionality will be implemented soon');
  }

  getTotalAmount(): number {
    return this.rows.reduce(
      (total, payment) => total + payment.paymentAmount,
      0
    );
  }
}
