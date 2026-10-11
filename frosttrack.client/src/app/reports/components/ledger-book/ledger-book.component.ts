import { Component, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { LedgerBookService } from '../../services/ledger-book.service';
import {
  ILedgerBookGroup,
  ILedgerBookSummary,
} from '../../interfaces/ledger-book.interface';
import { TransactionGroupService } from 'app/common/services/transaction-group.service';
import { ILookup } from '@core/models/lookup';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';

const ALL_GROUPS = '';

@Component({
  selector: 'app-ledger-book',
  templateUrl: './ledger-book.component.html',
  styleUrls: ['./ledger-book.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
  providers: [DecimalPipe],
})
export class LedgerBookComponent implements OnInit {
  readonly allGroups = ALL_GROUPS;

  form: UntypedFormGroup;
  groups: ILookup<string>[] = [];

  summary: ILedgerBookSummary | null = null;
  groupLedger: ILedgerBookGroup | null = null;

  isLoading = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private ledgerBookService: LedgerBookService,
    private transactionGroupService: TransactionGroupService,
    private toastr: ToastrService,
    private decimalPipe: DecimalPipe,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    this.form = this.fb.group({
      groupId: [ALL_GROUPS],
      fromDate: [this.firstDayOfMonth(), Validators.required],
      toDate: [todayInputFormat(), Validators.required],
    });
  }

  ngOnInit(): void {
    this.transactionGroupService.getLookup().subscribe({
      next: (response) => (this.groups = response),
    });
  }

  get showSummary(): boolean {
    return !this.isLoading && !!this.summary;
  }

  get showGroupLedger(): boolean {
    return !this.isLoading && !!this.groupLedger;
  }

  generateReport(): void {
    if (this.form.invalid) {
      this.toastr.error('Please select valid from and to dates', 'Error');
      return;
    }

    const fromDate = new Date(this.form.value.fromDate);
    const toDate = new Date(this.form.value.toDate);
    if (fromDate > toDate) {
      this.toastr.error('From date cannot be after To date', 'Error');
      return;
    }

    const groupId: string = this.form.value.groupId;
    this.isLoading = true;
    this.summary = null;
    this.groupLedger = null;

    if (groupId === ALL_GROUPS) {
      this.ledgerBookService.getSummary(fromDate, toDate).subscribe({
        next: (data) => {
          this.summary = data;
          this.isLoading = false;
        },
        error: (error: any) => this.onError(error),
      });
    } else {
      this.ledgerBookService.getGroupLedger(groupId, fromDate, toDate).subscribe({
        next: (data) => {
          this.groupLedger = data;
          this.isLoading = false;
        },
        error: (error: any) => this.onError(error),
      });
    }
  }

  /** Drill down from the All Groups summary into one group's ledger. */
  openGroup(groupId: string): void {
    this.form.patchValue({ groupId });
    this.generateReport();
  }

  backToSummary(): void {
    this.form.patchValue({ groupId: ALL_GROUPS });
    this.generateReport();
  }

  reset(): void {
    this.form.patchValue({
      groupId: ALL_GROUPS,
      fromDate: this.firstDayOfMonth(),
      toDate: todayInputFormat(),
    });
    this.summary = null;
    this.groupLedger = null;
  }

  setQuickRange(type: 'today' | 'month' | 'lastMonth'): void {
    const now = new Date();
    const todayStr = todayInputFormat();

    if (type === 'today') {
      this.form.patchValue({ fromDate: todayStr, toDate: todayStr });
    } else if (type === 'month') {
      this.form.patchValue({ fromDate: this.firstDayOfMonth(), toDate: todayStr });
    } else if (type === 'lastMonth') {
      const first = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      const last = new Date(now.getFullYear(), now.getMonth(), 0);
      this.form.patchValue({
        fromDate: this.formatDate(first),
        toDate: this.formatDate(last),
      });
    }
  }

  /** Signed balance (Credit - Debit) shown as "1,234.00 Cr" / "1,234.00 Dr". */
  formatBalance(value: number): string {
    if (!value) return '0.00';
    const amount = this.decimalPipe.transform(Math.abs(value), '1.2-2');
    return `${amount} ${value > 0 ? 'Cr' : 'Dr'}`;
  }

  private onError(error: any): void {
    this.toastr.error(error?.error?.message || 'Failed to load ledger book', 'Error');
    console.error(error);
    this.isLoading = false;
  }

  private firstDayOfMonth(): string {
    const d = new Date();
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    return `${d.getFullYear()}-${month}-01`;
  }

  private formatDate(date: Date): string {
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }
}
