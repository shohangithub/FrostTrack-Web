import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';
import {
  ILedgerCheckItem,
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

  // Charge ledger check (ledger total vs independent calculation)
  ledgerCheck: ILedgerCheckItem[] | null = null;
  isCheckingLedger = false;
  isSyncingLedger = false;

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

  checkLedger(): void {
    this.isCheckingLedger = true;
    this.recurringChargeService.ledgerCheck(true).subscribe({
      next: (res) => {
        this.ledgerCheck = res;
        this.isCheckingLedger = false;
      },
      error: () => {
        this.toastr.error('Failed to check the charge ledger.');
        this.isCheckingLedger = false;
      },
    });
  }

  syncAllLedgers(): void {
    Swal.fire({
      title: 'Sync charge ledger for all bookings?',
      html: 'Posts every missing month, labour and correction for all active bookings (safe to repeat).',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes, Sync',
    }).then((result) => {
      if (!result.value) return;
      this.isSyncingLedger = true;
      this.recurringChargeService.syncAllLedgers().subscribe({
        next: (res) => {
          this.isSyncingLedger = false;
          this.toastr.success(
            `${res.entriesPosted} entr(ies) posted for ${res.bookingsAffected} booking(s), ৳${res.amountPosted.toFixed(2)}`,
          );
          this.checkLedger();
        },
        error: () => {
          this.isSyncingLedger = false;
          this.toastr.error('Ledger sync failed.');
        },
      });
    });
  }

  rebuildLedger(): void {
    Swal.fire({
      title: 'Rebuild the whole charge ledger?',
      html:
        'Use this only while setting up (e.g. after changing the billing start date).<br/>' +
        'All current ledger rows are kept as LEGACY history (not counted) and every charge is posted again.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: SwalConfirm.confirmButtonColor,
      cancelButtonColor: SwalConfirm.cancelButtonColor,
      confirmButtonText: 'Yes, Rebuild',
    }).then((result) => {
      if (!result.value) return;
      this.isSyncingLedger = true;
      this.recurringChargeService.rebuildLedger().subscribe({
        next: (res) => {
          this.isSyncingLedger = false;
          this.toastr.success(
            `Ledger rebuilt: ${res.entriesPosted} entr(ies), ৳${res.amountPosted.toFixed(2)}`,
          );
          this.checkLedger();
        },
        error: () => {
          this.isSyncingLedger = false;
          this.toastr.error('Ledger rebuild failed.');
        },
      });
    });
  }

  syncBooking(item: ILedgerCheckItem): void {
    this.recurringChargeService.syncBookingLedger(item.bookingId).subscribe({
      next: (res) => {
        this.toastr.success(`Booking ${item.bookingNumber}: ${res.entriesPosted} entr(ies) posted`);
        this.checkLedger();
      },
      error: () => this.toastr.error('Ledger sync failed.'),
    });
  }
}
