import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  NgxDatatableModule,
  ColumnMode,
  SortType,
} from '@swimlane/ngx-datatable';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import Swal from 'sweetalert2';
import { SupplierPaymentService } from '../../services/supplier-payment.service';
import {
  ISupplierPaymentListResponse,
  ISupplierPaymentPaginationQuery,
} from '../../models/supplier-payment.interface';
import { PaginatedComponent } from '@core/base/paginated-component';

@Component({
  selector: 'app-supplier-payment-list',
  templateUrl: './supplier-payment-list.component.html',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NgxDatatableModule],
})
export class SupplierPaymentListComponent
  extends PaginatedComponent
  implements OnInit
{
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  rows: ISupplierPaymentListResponse[] = [];
  reorderable = true;
  activeStatus: string = 'active';

  columns = [
    { name: 'Payment Number', prop: 'paymentNumber', width: 150 },
    { name: 'Date', prop: 'paymentDate', width: 120 },
    { name: 'Type', prop: 'paymentType', width: 100 },
    { name: 'Supplier/Customer', prop: 'supplierName', width: 200 },
    { name: 'Amount', prop: 'paymentAmount', width: 120 },
    { name: 'Method', prop: 'paymentMethod', width: 100 },
    {
      name: 'Actions',
      prop: 'actions',
      width: 100,
      canAutoResize: false,
      sortable: false,
    },
  ];

  ColumnMode = ColumnMode;
  SortType = SortType;

  constructor(private supplierPaymentService: SupplierPaymentService) {
    super();
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loadPaginatedData(
      (query) => this.supplierPaymentService.getWithPagination(query),
      {
        additionalFilters: { status: this.activeStatus },
        onDataLoaded: (data) => {
          this.rows = data;
        },
      },
    );
  }

  setStatus(status: string) {
    this.activeStatus = status;
    this.currentPage = 1;
    this.loadData();
  }

  archivePayment(id: number): void {
    this.supplierPaymentService.archive(id).subscribe({
      next: () => {
        this.rows = this.rows.filter((r) => r.id !== id);
      },
      error: () => {},
    });
  }

  unarchivePayment(id: number): void {
    this.supplierPaymentService.unarchive(id).subscribe({
      next: () => {
        this.rows = this.rows.filter((r) => r.id !== id);
      },
      error: () => {},
    });
  }

  restorePayment(id: number): void {
    this.supplierPaymentService.restore(id).subscribe({
      next: () => {
        this.rows = this.rows.filter((r) => r.id !== id);
      },
      error: () => {},
    });
  }

  async permanentDeletePayment(id: number): Promise<void> {
    const result = await Swal.fire({
      title: 'Permanently delete this record?',
      text: 'This action cannot be undone!',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete!',
    });
    if (result.isConfirmed) {
      try {
        const success = await firstValueFrom(
          this.supplierPaymentService.permanentDelete(id),
        );
        if (success) {
          this.rows = this.rows.filter((r) => r.id !== id);
        }
      } catch (error) {
        console.error('Error permanently deleting payment:', error);
      }
    }
  }

  onSort(event: any): void {
    const sort = event.sorts[0];
    this.pagination.orderBy = sort.prop;
    this.pagination.isAscending = sort.dir === 'asc';
    this.loadData();
  }

  async deletePayment(id: number): Promise<void> {
    const result = await Swal.fire({
      title: 'Are you sure?',
      text: 'This payment record will be soft-deleted!',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!',
    });

    if (result.isConfirmed) {
      try {
        const success = await firstValueFrom(
          this.supplierPaymentService.softDelete(id),
        );
        if (success) {
          this.rows = this.rows.filter((r) => r.id !== id);
        }
      } catch (error) {
        console.error('Error deleting payment:', error);
      }
    }
  }
}
