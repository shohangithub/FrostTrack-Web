import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';
import { todayInputFormat } from 'app/utils/date-utils';
import { CustomerBillStatementService } from '../../services/customer-bill-statement.service';
import { ICustomerBillStatement } from '../../models/customer-bill-statement.interface';

/** Monthly (or any period) bill for a customer: bookings, deliveries with labour, payments and dues. */
@Component({
  selector: 'app-customer-bill-statement',
  templateUrl: './customer-bill-statement.component.html',
  styleUrls: [
    '../income-expense-report/income-expense-report.component.scss',
    './customer-bill-statement.component.scss',
  ],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgSelectModule,
    NgxPrintModule,
    ReportInvoiceHeaderComponent,
    ReportFooterComponent,
  ],
})
export class CustomerBillStatementComponent implements OnInit {
  reportForm: UntypedFormGroup;
  /** Only customers with contract (চুক্তি) bookings are billed on this statement. */
  customers: { value: number; text: string }[] = [];
  statement: ICustomerBillStatement | null = null;
  isLoading = false;
  today = new Date();

  constructor(
    private fb: UntypedFormBuilder,
    private billStatementService: CustomerBillStatementService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    const [monthStart, monthEnd] = this.monthRange(0);
    this.reportForm = this.fb.group(
      {
        customerId: [null, Validators.required],
        fromDate: [monthStart, Validators.required],
        toDate: [monthEnd > todayInputFormat() ? todayInputFormat() : monthEnd, Validators.required],
      },
      { validators: CustomerBillStatementComponent.rangeValidator },
    );
  }

  static rangeValidator(group: AbstractControl): ValidationErrors | null {
    const from = group.get('fromDate')?.value;
    const to = group.get('toDate')?.value;
    return from && to && from > to ? { invalidRange: true } : null;
  }

  ngOnInit(): void {
    this.billStatementService.getContractCustomers().subscribe({
      next: (data) => (this.customers = data),
      error: () => this.toastr.error('Failed to load customers', 'Error'),
    });
  }

  /** Quick pick: 0 = this month, -1 = last month. */
  selectMonth(offset: number): void {
    const [from, to] = this.monthRange(offset);
    this.reportForm.patchValue({ fromDate: from, toDate: to });
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      this.toastr.error(
        this.reportForm.hasError('invalidRange')
          ? 'From date cannot be after To date'
          : 'Please select a customer and date range',
        'Error',
      );
      return;
    }

    const { customerId, fromDate, toDate } = this.reportForm.value;
    this.isLoading = true;
    this.statement = null;

    this.billStatementService.getStatement(customerId, fromDate, toDate).subscribe({
      next: (data) => {
        this.statement = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(err?.error?.message || 'Failed to load bill statement', 'Error');
        this.isLoading = false;
      },
    });
  }

  reset(): void {
    this.statement = null;
  }

  get stockRentTotal(): number {
    return (this.statement?.stockLines ?? []).reduce((s, l) => s + l.rentThisPeriod, 0);
  }

  get closingStockTotal(): number {
    return (this.statement?.stockLines ?? []).reduce((s, l) => s + l.closingStock, 0);
  }

  /** Delivery labour column is shown only when some delivery in the period has labour. */
  get hasDeliveryLabour(): boolean {
    return (this.statement?.deliveries ?? []).some((d) => d.labourCharge > 0);
  }

  get hasActivity(): boolean {
    const s = this.statement;
    return !!s && (s.stockLines.length > 0 || s.bookings.length > 0 || s.deliveries.length > 0 || s.chargeEntries.length > 0);
  }

  chargeLabel(chargeType: string, cycleNo?: number | null): string {
    switch (chargeType) {
      case 'RENT_CYCLE':
        return `Rent - month ${cycleNo ?? ''}`;
      case 'BOOKING_LABOUR':
        return 'Booking labour';
      case 'DELIVERY_LABOUR':
        return 'Delivery labour';
      case 'DELIVERY_ADJUSTMENT':
        return 'Delivery adjustment';
      case 'MANUAL_ADJUSTMENT':
        return 'Adjustment (+)';
      default:
        return chargeType;
    }
  }

  private monthRange(offset: number): [string, string] {
    const now = new Date();
    const first = new Date(now.getFullYear(), now.getMonth() + offset, 1);
    const last = new Date(now.getFullYear(), now.getMonth() + offset + 1, 0);
    return [this.toInputDate(first), this.toInputDate(last)];
  }

  private toInputDate(d: Date): string {
    const month = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${d.getFullYear()}-${month}-${day}`;
  }
}
