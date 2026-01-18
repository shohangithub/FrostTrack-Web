import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Configuration } from '@config/configuration';
import { LayoutService } from '@core/service/layout.service';
import {
  DatatableComponent,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  ICustomerDueSummaryResponse,
  ICustomerDueDetailResponse,
} from 'app/booking/models/booking.interface';
import { BookingService } from 'app/booking/services/booking.service';
import { ToastrService } from 'ngx-toastr';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-customer-due-list',
  templateUrl: './customer-due-list.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    NgxDatatableModule,
    DatePipe,
    DecimalPipe,
    CommonModule,
    FormsModule,
  ],
})
export class CustomerDueListComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  data: ICustomerDueSummaryResponse[] = [];
  filteredData: ICustomerDueSummaryResponse[] = [];
  expandedCustomers: Map<number, ICustomerDueDetailResponse[]> = new Map();
  loadingIndicator = true;
  scrollBarHorizontal = window.innerWidth < 1200;
  reorderable = true;
  expanded: any = {};

  // Filters
  selectedStatus: string = 'all';
  searchText: string = '';
  private searchSubject: Subject<string> = new Subject<string>();

  constructor(
    private router: Router,
    private toastr: ToastrService,
    private layoutService: LayoutService,
    private bookingService: BookingService,
  ) {
    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.fetchData();

    this.searchSubject
      .pipe(
        debounceTime(Configuration.SEARCH_DEBOUNCE_TIME),
        distinctUntilChanged(),
      )
      .subscribe(() => {
        this.applyFilters();
      });
  }

  fetchData() {
    this.loadingIndicator = true;
    this.bookingService.getCustomerDueSummary().subscribe({
      next: (response: ICustomerDueSummaryResponse[]) => {
        this.data = response;
        this.applyFilters();
        this.loadingIndicator = false;
      },
      error: (error) => {
        console.error('Failed to load customer due data:', error);
        this.toastr.error('Failed to load customer due data');
        this.loadingIndicator = false;
      },
    });
  }

  applyFilters() {
    let filtered = [...this.data];

    // Apply status filter
    if (this.selectedStatus !== 'all') {
      filtered = filtered.filter((item) => item.status === this.selectedStatus);
    }

    // Apply search filter
    if (this.searchText.trim()) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(
        (item) =>
          item.customerName.toLowerCase().includes(search) ||
          item.customerMobile.toLowerCase().includes(search),
      );
    }

    this.filteredData = filtered;
  }

  onSearch(event: any) {
    this.searchText = event.target.value;
    this.searchSubject.next(this.searchText);
  }

  onStatusFilter(event: any) {
    this.selectedStatus = event.target.value;
    this.applyFilters();
  }

  onDetailToggle(event: any) {
    const customerId = event.value.customerId;

    if (event.type === 'row' && event.value) {
      if (!this.expandedCustomers.has(customerId)) {
        // Load customer due details
        this.bookingService.getCustomerDueDetail(customerId).subscribe({
          next: (response: ICustomerDueDetailResponse[]) => {
            this.expandedCustomers.set(customerId, response);
          },
          error: (error) => {
            console.error('Failed to load customer due details:', error);
            this.toastr.error('Failed to load customer due details');
          },
        });
      }
    }
  }

  getCustomerDetails(customerId: number): ICustomerDueDetailResponse[] {
    return this.expandedCustomers.get(customerId) || [];
  }

  toggleExpandRow(row: any) {
    this.table.rowDetail.toggleExpandRow(row);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'danger':
        return 'badge bg-danger';
      case 'warning':
        return 'badge bg-warning';
      default:
        return 'badge bg-success';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'danger':
        return 'Overdue (30+ days)';
      case 'warning':
        return 'Due Soon (25+ days)';
      default:
        return 'Normal';
    }
  }

  printCustomerDue(customerId: number) {
    this.router.navigate(['/booking/customer-due-print', customerId]);
  }

  getTotalDeliveryAmount(deliveries: any[]): number {
    if (!deliveries) return 0;
    return deliveries.reduce((sum, delivery) => {
      return (
        sum +
        delivery.chargeAmount +
        delivery.labourCharge +
        delivery.adjustmentValue -
        delivery.discountAmount
      );
    }, 0);
  }

  getTotalAmount(): number {
    return this.filteredData.reduce((sum, item) => sum + item.totalAmount, 0);
  }

  getTotalPaid(): number {
    return this.filteredData.reduce((sum, item) => sum + item.totalPaid, 0);
  }

  getTotalDue(): number {
    return this.filteredData.reduce((sum, item) => sum + item.totalDue, 0);
  }
}
