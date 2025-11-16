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
import { ISupplierPaymentListResponse } from '../../models/supplier-payment.interface';
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
        onDataLoaded: (data) => {
          this.rows = data;
        },
      }
    );
  }

  async deletePayment(id: number): Promise<void> {
    const result = await Swal.fire({
      title: 'Are you sure?',
      text: 'This payment will be deleted permanently!',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Yes, delete it!',
    });

    if (result.isConfirmed) {
      try {
        const success = await firstValueFrom(
          this.supplierPaymentService.remove(id)
        );
        if (success) {
          this.loadData();
        }
      } catch (error) {
        console.error('Error deleting payment:', error);
      }
    }
  }
}
