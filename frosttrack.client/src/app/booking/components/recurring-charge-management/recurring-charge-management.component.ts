import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';
import {
  IRecurringChargePreview,
  IRecurringChargeRunResponse,
} from 'app/booking/models/booking.interface';
import { RecurringChargeManagementService } from 'app/booking/services/recurring-charge-management.service';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-recurring-charge-management',
  templateUrl: './recurring-charge-management.component.html',
  styleUrls: [],
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, DecimalPipe],
})
export class RecurringChargeManagementComponent implements OnInit {
  today = new Date().toISOString().substring(0, 10);
  asOfDate: string = this.today;
  notes: string = '';

  isLoadingPreview = false;
  isApplying = false;
  isLoadingHistory = false;

  preview: IRecurringChargePreview | null = null;
  history: IRecurringChargeRunResponse[] = [];

  constructor(
    private recurringChargeService: RecurringChargeManagementService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {}

  ngOnInit(): void {
    this.layoutService.loadCurrentRoute();
    this.loadHistory();
  }

  loadPreview(): void {
    this.isLoadingPreview = true;
    this.preview = null;
    this.recurringChargeService.preview(this.asOfDate).subscribe({
      next: (res) => {
        this.preview = res;
        this.isLoadingPreview = false;
      },
      error: () => {
        this.toastr.error('Failed to load preview.');
        this.isLoadingPreview = false;
      },
    });
  }

  applyRecurringCharge(): void {
    Swal.fire({
      title: 'Apply Manual Recurring Charge?',
      html: `This will update recurring-charge records for all eligible bookings as of <strong>${this.asOfDate}</strong>.<br/>This action cannot be undone.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes, Apply',
      cancelButtonText: 'Cancel',
    }).then((result) => {
      if (!result.value) return;
      this.isApplying = true;
      this.recurringChargeService
        .apply({ asOfDate: this.asOfDate, notes: this.notes || undefined })
        .subscribe({
          next: (res) => {
            this.isApplying = false;
            if (res.status === 'SUCCESS') {
              Swal.fire({
                title: 'Recurring Charge Applied',
                html: `<strong>${res.affectedCount}</strong> booking(s) updated.<br/>Total: ৳${res.totalRecurringChargeAmount.toFixed(2)}`,
                icon: 'success',
                confirmButtonColor: SwalConfirm.confirmButtonColor,
              });
            } else {
              this.toastr.warning(
                `Recurring-charge run finished with status: ${res.status}. ${res.errorMessage ?? ''}`,
              );
            }
            this.preview = null;
            this.loadHistory();
          },
          error: () => {
            this.isApplying = false;
            this.toastr.error('Failed to apply recurring charge.');
          },
        });
    });
  }

  loadHistory(): void {
    this.isLoadingHistory = true;
    this.recurringChargeService.getHistory(30).subscribe({
      next: (res) => {
        this.history = res;
        this.isLoadingHistory = false;
      },
      error: () => {
        this.isLoadingHistory = false;
      },
    });
  }
}
