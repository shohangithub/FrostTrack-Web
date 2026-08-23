import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Configuration } from '@config/configuration';
import { LayoutService } from '@core/service/layout.service';
import {
  ICustomerDueSummaryResponse,
  ICustomerDueDetailResponse,
  IRecurringChargeEntryResponse,
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
  imports: [DatePipe, DecimalPipe, CommonModule, FormsModule],
})
export class CustomerDueListComponent implements OnInit {
  data: ICustomerDueSummaryResponse[] = [];
  filteredData: ICustomerDueSummaryResponse[] = [];
  expandedCustomers: Map<number, ICustomerDueDetailResponse[]> = new Map();
  expandedLoadingSet: Set<number> = new Set();
  expandedCustomerIds: Set<number> = new Set();
  expandedRecurringChargeBookingIds: Set<string> = new Set();
  loadingIndicator = true;

  selectedStatus: string = 'all';
  searchText: string = '';
  private searchSubject: Subject<string> = new Subject<string>();

  constructor(
    private router: Router,
    private toastr: ToastrService,
    private layoutService: LayoutService,
    private bookingService: BookingService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.fetchData();
    this.searchSubject
      .pipe(
        debounceTime(Configuration.SEARCH_DEBOUNCE_TIME),
        distinctUntilChanged(),
      )
      .subscribe(() => this.applyFilters());
  }

  fetchData() {
    this.loadingIndicator = true;
    this.expandedCustomers.clear();
    this.expandedLoadingSet.clear();
    this.expandedCustomerIds.clear();
    this.expandedRecurringChargeBookingIds.clear();
    this.bookingService.getCustomerDueSummary().subscribe({
      next: (response) => {
        this.data = response;
        this.applyFilters();
        this.loadingIndicator = false;
      },
      error: () => {
        this.toastr.error('Failed to load customer due data');
        this.loadingIndicator = false;
      },
    });
  }

  applyFilters() {
    let filtered = [...this.data];
    if (this.selectedStatus !== 'all') {
      filtered = filtered.filter((item) => item.status === this.selectedStatus);
    }
    if (this.searchText.trim()) {
      const search = this.searchText.toLowerCase();
      filtered = filtered.filter(
        (item) =>
          item.customerName.toLowerCase().includes(search) ||
          item.customerMobile.toLowerCase().includes(search) ||
          (item.customerAddress || '').toLowerCase().includes(search),
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

  toggleExpand(row: ICustomerDueSummaryResponse) {
    const id = row.customerId;
    if (this.expandedCustomerIds.has(id)) {
      this.expandedCustomerIds.delete(id);
    } else {
      this.expandedCustomerIds.add(id);
      if (!this.expandedCustomers.has(id) && !this.expandedLoadingSet.has(id)) {
        this.expandedLoadingSet.add(id);
        this.bookingService.getCustomerDueDetail(id).subscribe({
          next: (response) => {
            this.expandedCustomers.set(id, response);
            this.expandedLoadingSet.delete(id);
          },
          error: () => {
            this.toastr.error('Failed to load customer due details');
            this.expandedLoadingSet.delete(id);
          },
        });
      }
    }
  }

  isExpanded(customerId: number): boolean {
    return this.expandedCustomerIds.has(customerId);
  }

  isDetailLoading(customerId: number): boolean {
    return this.expandedLoadingSet.has(customerId);
  }

  getCustomerDetails(customerId: number): ICustomerDueDetailResponse[] {
    return this.expandedCustomers.get(customerId) || [];
  }

  toggleRecurringChargeHistory(bookingId: string): void {
    if (this.expandedRecurringChargeBookingIds.has(bookingId)) {
      this.expandedRecurringChargeBookingIds.delete(bookingId);
    } else {
      this.expandedRecurringChargeBookingIds.add(bookingId);
    }
  }

  isRecurringChargeHistoryExpanded(bookingId: string): boolean {
    return this.expandedRecurringChargeBookingIds.has(bookingId);
  }

  sumRecurringChargeEntries(entries: IRecurringChargeEntryResponse[]): number {
    return entries?.reduce((sum, e) => sum + e.amount, 0) ?? 0;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'danger':
        return 'badge bg-danger';
      case 'warning':
        return 'badge bg-warning text-dark';
      default:
        return 'badge bg-success';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'danger':
        return 'Overdue';
      case 'warning':
        return 'Due Soon';
      default:
        return 'Current';
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'danger':
        return 'warning';
      case 'warning':
        return 'schedule';
      default:
        return 'check_circle';
    }
  }

  printCustomerDue(customerId: number) {
    this.router.navigate(['/booking/customer-due-print', customerId]);
  }

  // ── Summary stats ──────────────────────────────────────────────────────────

  getBookingTotalLabour(booking: any): number {
    if (!booking || !booking.deliveries) return 0;
    return booking.deliveries.reduce((sum: number, del: any) => sum + Number(del.labourCharge || 0), 0);
  }

  getTotalCustomers(): number {
    return this.filteredData.length;
  }

  getTotalOverdue(): number {
    return this.filteredData.filter((x) => x.status === 'danger').length;
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

  getTotalPendingRecurringCharge(): number {
    return this.filteredData.reduce(
      (sum, item) => sum + (item.pendingRecurringChargeAmount ?? 0),
      0,
    );
  }

  // ── Export CSV ────────────────────────────────────────────────────────────

  exportCsv() {
    const headers = [
      'Customer Name',
      'Mobile',
      'Address',
      'Bookings',
      'Opening Balance',
      'Total Billed',
      'Total Paid',
      'Outstanding Due',
      'Pending Recurring Charge',
      'Last Payment Date',
      'Days Since Last Payment',
      'Status',
      'Oldest Booking',
      'Days Since Oldest Booking',
    ];
    const rows = this.filteredData.map((r) => [
      r.customerName,
      r.customerMobile,
      r.customerAddress,
      r.totalBookings,
      r.openingBalance.toFixed(2),
      r.totalAmount.toFixed(2),
      r.totalPaid.toFixed(2),
      r.totalDue.toFixed(2),
      (r.pendingRecurringChargeAmount ?? 0).toFixed(2),
      r.lastPaymentDate ? r.lastPaymentDate.substring(0, 10) : 'No Payments',
      r.daysSinceLastPayment,
      this.getStatusText(r.status),
      r.oldestBookingDate ? r.oldestBookingDate.substring(0, 10) : '',
      r.daysSinceOldestBooking,
    ]);
    const csvContent = [headers, ...rows]
      .map((row) =>
        row.map((v) => `"${String(v).replace(/"/g, '""')}"`).join(','),
      )
      .join('\n');
    const blob = new Blob(['\uFEFF' + csvContent], {
      type: 'text/csv;charset=utf-8;',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `customer-due-${new Date().toISOString().substring(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
