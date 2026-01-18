import { DatePipe, CommonModule, DecimalPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NgxPrintModule } from 'ngx-print';
import { BookingService } from '../../services/booking.service';
import {
  ICustomerDueSummaryResponse,
  ICustomerDueDetailResponse,
} from '../../models/booking.interface';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';

@Component({
  selector: 'app-customer-due-print',
  templateUrl: './customer-due-print.component.html',
  styleUrls: ['./customer-due-print.component.scss'],
  standalone: true,
  imports: [
    NgxPrintModule,
    DatePipe,
    DecimalPipe,
    CommonModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class CustomerDuePrintComponent implements OnInit {
  customerDueDetails: ICustomerDueDetailResponse[] = [];
  customerInfo: ICustomerDueSummaryResponse | null = null;
  loadingIndicator = true;
  customerId: number = 0;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private bookingService: BookingService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.customerId = +params['id'];
      if (this.customerId) {
        this.loadCustomerDueData();
      }
    });
  }

  loadCustomerDueData(): void {
    this.loadingIndicator = true;

    // Load customer summary first
    this.bookingService.getCustomerDueSummary().subscribe({
      next: (summaries: ICustomerDueSummaryResponse[]) => {
        this.customerInfo =
          summaries.find((s) => s.customerId === this.customerId) || null;

        // Then load detailed due information
        this.bookingService.getCustomerDueDetail(this.customerId).subscribe({
          next: (response: ICustomerDueDetailResponse[]) => {
            this.customerDueDetails = response;
            this.loadingIndicator = false;
          },
          error: (error) => {
            console.error('Failed to load customer due details:', error);
            this.loadingIndicator = false;
            this.toastr.error('Failed to load customer due details');
          },
        });
      },
      error: (error) => {
        console.error('Failed to load customer info:', error);
        this.loadingIndicator = false;
        this.toastr.error('Failed to load customer information');
      },
    });
  }

  printInvoice(): void {
    window.print();
  }

  goBack(): void {
    this.router.navigate(['/booking/customer-due-list']);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'danger':
        return 'status-danger';
      case 'warning':
        return 'status-warning';
      default:
        return 'status-normal';
    }
  }

  getStatusText(status: string): string {
    switch (status) {
      case 'danger':
        return 'অতিরিক্ত বকেয়া (৩০+ দিন)';
      case 'warning':
        return 'সতর্কতা (২৫+ দিন)';
      default:
        return 'স্বাভাবিক';
    }
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

  getTotalDeliveryPaid(deliveries: any[]): number {
    if (!deliveries) return 0;
    return deliveries.reduce((sum, delivery) => {
      return sum + delivery.paidAmount;
    }, 0);
  }

  getTotalDeliveryDue(deliveries: any[]): number {
    if (!deliveries) return 0;
    return deliveries.reduce((sum, delivery) => {
      return sum + delivery.dueAmount;
    }, 0);
  }

  getGrandTotal(): number {
    return this.customerDueDetails.reduce((sum, booking) => {
      return sum + booking.totalAmount;
    }, 0);
  }

  getGrandTotalPaid(): number {
    return this.customerDueDetails.reduce((sum, booking) => {
      return sum + booking.totalPaid;
    }, 0);
  }

  getGrandTotalDue(): number {
    return this.customerDueDetails.reduce((sum, booking) => {
      return sum + booking.totalDue;
    }, 0);
  }
}
