import { DatePipe, DecimalPipe, CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';
import {
  SeasonArchiveHistoryResponse,
  SeasonArchivePreviewResponse,
  SeasonArchiveResultResponse,
} from '../../models/season-archive.model';
import { SeasonArchiveService } from '../../services/season-archive.service';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-season-archive',
  templateUrl: './season-archive.component.html',
  styleUrls: ['./season-archive.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, DecimalPipe],
})
export class SeasonArchiveComponent implements OnInit {
  today = new Date().toISOString().substring(0, 10);
  cutoffDate: string = this.today;
  seasonTitle: string = `Season ${new Date().getFullYear()} Closing`;
  notes: string = '';

  isLoadingPreview = false;
  isExecuting = false;
  isLoadingHistory = false;

  preview: SeasonArchivePreviewResponse | null = null;
  history: SeasonArchiveHistoryResponse[] = [];
  lastResult: SeasonArchiveResultResponse | null = null;

  constructor(
    private readonly seasonArchiveService: SeasonArchiveService,
    private readonly toastr: ToastrService,
    private readonly layoutService: LayoutService,
  ) {}

  ngOnInit(): void {
    this.layoutService.loadCurrentRoute();
    this.loadPreview();
    this.loadHistory();
  }

  loadPreview(): void {
    this.isLoadingPreview = true;
    this.preview = null;
    this.seasonArchiveService.getPreview(this.cutoffDate).subscribe({
      next: (res) => {
        this.preview = res;
        this.isLoadingPreview = false;
      },
      error: (err) => {
        this.toastr.error(err?.error?.message || 'Failed to load archive preview.');
        this.isLoadingPreview = false;
      },
    });
  }

  confirmAndExecute(): void {
    if (!this.seasonTitle || this.seasonTitle.trim().length === 0) {
      this.toastr.warning('Please enter a season title.');
      return;
    }

    if (!this.preview) {
      this.toastr.warning('Please load preview first.');
      return;
    }

    Swal.fire({
      title: 'Close Season & Archive Operational Data?',
      html: `
        <div class="text-start fs-14">
          <div class="alert alert-warning mb-3">
            <i class="fas fa-exclamation-triangle me-2"></i>
            <strong>Critical Operational Action:</strong> This will archive completed records up to <strong>${this.cutoffDate}</strong> and roll forward all rest balances into the new season.
          </div>
          <ul class="list-unstyled mb-3">
            <li class="mb-1"><i class="fas fa-archive text-warning me-2"></i><strong>${this.preview.completedBookingsCount}</strong> completed booking(s) will be archived</li>
            <li class="mb-1"><i class="fas fa-boxes text-info me-2"></i><strong>${this.preview.openBookingsToCarryForwardCount}</strong> open booking(s) with <strong>${this.preview.totalRemainingStockBags} bags</strong> will carry forward as active stock</li>
            <li class="mb-1"><i class="fas fa-user-check text-primary me-2"></i><strong>${this.preview.totalCustomersCount}</strong> customer(s) net balance (৳${this.preview.totalCustomerNetDue.toFixed(2)}) will carry forward as opening balance</li>
            <li class="mb-1"><i class="fas fa-money-bill-wave text-success me-2"></i>Cash in hand (৳${this.preview.currentCashInHand.toFixed(2)}) will roll forward to Day 1 opening balance</li>
            <li class="mb-1"><i class="fas fa-file-invoice text-muted me-2"></i>${this.preview.deliveriesToArchiveCount} deliveries and ${this.preview.transactionsToArchiveCount} transactions will be archived</li>
          </ul>
          <div class="mb-2 text-danger fw-bold">To confirm this action, please type <span class="badge bg-danger">CONFIRM-ARCHIVE</span> below:</div>
        </div>
      `,
      input: 'text',
      inputPlaceholder: 'Type CONFIRM-ARCHIVE',
      inputAttributes: {
        autocapitalize: 'off',
        autocomplete: 'off',
      },
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#dc2626',
      cancelButtonColor: '#64748b',
      confirmButtonText: '<i class="fas fa-lock me-1"></i> Confirm & Execute Archive',
      cancelButtonText: 'Cancel',
      preConfirm: (value) => {
        if (value !== 'CONFIRM-ARCHIVE') {
          Swal.showValidationMessage('Confirmation code does not match! Please type CONFIRM-ARCHIVE exactly.');
          return false;
        }
        return value;
      },
    }).then((result) => {
      if (!result.isConfirmed) return;

      this.isExecuting = true;
      this.seasonArchiveService
        .executeArchive({
          cutoffDate: this.cutoffDate,
          seasonTitle: this.seasonTitle.trim(),
          confirmationCode: 'CONFIRM-ARCHIVE',
          notes: this.notes || undefined,
        })
        .subscribe({
          next: (res) => {
            this.isExecuting = false;
            this.lastResult = res;

            Swal.fire({
              title: 'Season Archived Successfully!',
              html: `
                <div class="text-start">
                  <p class="mb-2 text-success fw-bold"><i class="fas fa-check-circle me-1"></i> ${res.message}</p>
                  <table class="table table-bordered table-sm mt-3">
                    <tr><th>Batch Code:</th><td class="fw-bold">${res.batchCode}</td></tr>
                    <tr><th>Carried Forward Stock:</th><td>${res.carriedForwardStockBags} Bags (${res.carriedForwardBookingsCount} Open Bookings)</td></tr>
                    <tr><th>Carried Customer Due:</th><td>৳${res.carriedForwardCustomerDue.toFixed(2)}</td></tr>
                    <tr><th>Carried Cash Balance:</th><td>৳${res.carriedForwardCashBalance.toFixed(2)}</td></tr>
                    <tr><th>Carried Bank Balances:</th><td>৳${res.carriedForwardBankBalance.toFixed(2)}</td></tr>
                  </table>
                  <div class="alert alert-info mt-2 mb-0 fs-13">
                    <i class="fas fa-info-circle me-1"></i> All operational reports (Daily Stock Book, Cash Book, Bank Book) are now reset and ready with fresh carry-over figures!
                  </div>
                </div>
              `,
              icon: 'success',
              confirmButtonColor: SwalConfirm.confirmButtonColor,
            });

            this.loadPreview();
            this.loadHistory();
          },
          error: (err) => {
            this.isExecuting = false;
            const errMsg = err?.error?.message || 'Failed to execute season archive.';
            Swal.fire({
              title: 'Archive Failed',
              text: errMsg,
              icon: 'error',
              confirmButtonColor: SwalConfirm.confirmButtonColor,
            });
          },
        });
    });
  }

  loadHistory(): void {
    this.isLoadingHistory = true;
    this.seasonArchiveService.getHistory().subscribe({
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
