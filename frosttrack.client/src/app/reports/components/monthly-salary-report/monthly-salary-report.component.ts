import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { SalaryPaymentService } from 'app/salary-payment/services/salary-payment.service';
import { EmployeeService } from 'app/common/services/employee.service';
import { ISalaryPaymentList } from 'app/salary-payment/models/salary-payment.interface';
import { ILookup } from '@core/models/lookup';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';

@Component({
  selector: 'app-monthly-salary-report',
  templateUrl: './monthly-salary-report.component.html',
  styleUrls: ['./monthly-salary-report.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    NgSelectModule,
    NgxPrintModule,
    ReactiveFormsModule,
    ReportFooterComponent,
    ReportInvoiceHeaderComponent,
  ],
})
export class MonthlySalaryReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  salaryPayments: ISalaryPaymentList[] = [];
  isLoading = false;
  showReport = false;

  employees: ILookup<number>[] = [];

  // Month and year options
  months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' },
  ];

  years: number[] = [];

  // Summary calculations
  totalBasicSalary = 0;
  totalBonus = 0;
  totalDeduction = 0;
  totalNetAmount = 0;
  totalEmployees = 0;

  selectedMonth = 0;
  selectedYear = 0;

  constructor(
    private fb: UntypedFormBuilder,
    private salaryPaymentService: SalaryPaymentService,
    private employeeService: EmployeeService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    // Generate year options (current year and 5 years back)
    const currentYear = new Date().getFullYear();
    for (let i = 0; i <= 5; i++) {
      this.years.push(currentYear - i);
    }

    // Initialize form with current month and year
    const currentMonth = new Date().getMonth() + 1;

    this.reportForm = this.fb.group({
      month: [currentMonth, Validators.required],
      year: [currentYear, Validators.required],
      employeeId: [null],
    });
  }

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.employeeService.getLookup().subscribe({
      next: (data: ILookup<number>[]) => {
        this.employees = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load employees', 'Error');
        console.error('Error loading employees:', error);
      },
    });
  }

  generateReport(): void {
    if (this.reportForm.invalid) {
      this.reportForm.markAllAsTouched();
      return;
    }

    const formValue = this.reportForm.value;
    this.selectedMonth = formValue.month;
    this.selectedYear = formValue.year;

    this.isLoading = true;

    this.salaryPaymentService
      .getPaymentHistory(
        formValue.employeeId || undefined,
        this.getStartDate(formValue.month, formValue.year),
        this.getEndDate(formValue.month, formValue.year),
      )
      .subscribe({
        next: (data: ISalaryPaymentList[]) => {
          this.salaryPayments = data;
          this.calculateSummary();
          this.showReport = true;
          this.isLoading = false;

          if (data.length === 0) {
            this.toastr.info(
              'No salary payments found for the selected criteria',
              'Info',
            );
          }
        },
        error: (error: any) => {
          this.toastr.error('Failed to generate report', 'Error');
          console.error('Error generating report:', error);
          this.isLoading = false;
        },
      });
  }

  private getStartDate(month: number, year: number): Date {
    return new Date(year, month - 1, 1, 0, 0, 0);
  }

  private getEndDate(month: number, year: number): Date {
    return new Date(year, month, 0, 23, 59, 59);
  }

  calculateSummary(): void {
    this.totalBasicSalary = 0;
    this.totalNetAmount = 0;
    this.totalEmployees = this.salaryPayments.length;

    this.salaryPayments.forEach((payment) => {
      this.totalBasicSalary += payment.basicSalary;
      this.totalNetAmount += payment.netAmount;
    });
  }

  reset(): void {
    this.reportForm.reset({
      month: new Date().getMonth() + 1,
      year: new Date().getFullYear(),
      employeeId: null,
    });
    this.salaryPayments = [];
    this.showReport = false;
    this.totalBasicSalary = 0;
    this.totalBonus = 0;
    this.totalDeduction = 0;
    this.totalNetAmount = 0;
    this.totalEmployees = 0;
  }

  getMonthName(monthValue: number): string {
    const month = this.months.find((m) => m.value === monthValue);
    return month ? month.label : '';
  }

  getReportTitle(): string {
    return `মাসিক বেতন রিপোর্ট - ${this.getMonthName(this.selectedMonth)} ${this.selectedYear}`;
  }

  getSelectedEmployeeName(): string {
    const employeeId = this.reportForm.get('employeeId')?.value;
    if (!employeeId) return 'সকল কর্মচারী';

    const employee = this.employees.find((e) => e.value === employeeId);
    return employee ? employee.text : 'সকল কর্মচারী';
  }

  convertToMonthName(period: string): string {
    // Period format is "MM/YYYY"
    const parts = period.split('/');
    if (parts.length !== 2) return period;

    const monthNum = parseInt(parts[0], 10);
    const year = parts[1];
    const monthName =
      this.months.find((m) => m.value === monthNum)?.label || parts[0];

    return `${monthName} ${year}`;
  }
}
