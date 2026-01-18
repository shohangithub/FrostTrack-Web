import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  NgxDatatableModule,
  DatatableComponent,
} from '@swimlane/ngx-datatable';
import { ToastrService } from 'ngx-toastr';
import { DeliveryChallanService } from '../../services/delivery-challan.service';
import { IDeliveryChallanListResponse } from '../../models/delivery-challan.interface';
import { LayoutService } from '@core/service/layout.service';
import { debounceTime, Subject } from 'rxjs';

@Component({
  selector: 'app-delivery-challan-list',
  templateUrl: './delivery-challan-list.component.html',
  styleUrls: [],
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule],
})
export class DeliveryChallanListComponent implements OnInit {
  @ViewChild(DatatableComponent) table!: DatatableComponent;

  challans: IDeliveryChallanListResponse[] = [];
  filteredChallans: IDeliveryChallanListResponse[] = [];

  isLoading = false;
  searchText = '';
  selectedStatus = 'All';

  statusOptions = ['All', 'Pending', 'In Transit', 'Delivered', 'Cancelled'];

  private searchSubject = new Subject<string>();

  constructor(
    private challanService: DeliveryChallanService,
    private router: Router,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    this.searchSubject.pipe(debounceTime(300)).subscribe((searchValue) => {
      this.searchText = searchValue;
      this.applyFilters();
    });
  }

  ngOnInit(): void {
    this.loadChallans();
  }

  loadChallans(): void {
    this.isLoading = true;
    this.challanService.getList().subscribe({
      next: (data: IDeliveryChallanListResponse[]) => {
        this.challans = data;
        this.filteredChallans = data;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load delivery challans', 'Error');
        console.error('Error loading challans:', error);
        this.isLoading = false;
      },
    });
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
  }

  onStatusChange(): void {
    this.applyFilters();
  }

  applyFilters(): void {
    let filtered = this.challans;

    // Filter by status
    if (this.selectedStatus !== 'All') {
      filtered = filtered.filter((c) => c.status === this.selectedStatus);
    }

    // Filter by search text
    if (this.searchText) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(
        (c) =>
          c.challanNumber.toLowerCase().includes(search) ||
          c.vehicleNumber.toLowerCase().includes(search) ||
          c.driverName?.toLowerCase().includes(search) ||
          c.destination?.toLowerCase().includes(search),
      );
    }

    this.filteredChallans = filtered;
  }

  addNew(): void {
    this.router.navigate(['/product-delivery/challan/add']);
  }

  edit(id: string): void {
    this.router.navigate(['/product-delivery/challan/edit', id]);
  }

  print(id: string): void {
    this.router.navigate(['/product-delivery/challan/print', id]);
  }

  delete(id: string): void {
    if (confirm('Are you sure you want to delete this delivery challan?')) {
      this.challanService.remove(id).subscribe({
        next: () => {
          this.loadChallans();
        },
        error: (error: any) => {
          console.error('Error deleting challan:', error);
        },
      });
    }
  }

  updateStatus(id: string, status: string): void {
    this.challanService.updateStatus(id, status).subscribe({
      next: () => {
        this.loadChallans();
      },
      error: (error: any) => {
        console.error('Error updating status:', error);
      },
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'badge bg-warning';
      case 'In Transit':
        return 'badge bg-info';
      case 'Delivered':
        return 'badge bg-success';
      case 'Cancelled':
        return 'badge bg-danger';
      default:
        return 'badge bg-secondary';
    }
  }
}
