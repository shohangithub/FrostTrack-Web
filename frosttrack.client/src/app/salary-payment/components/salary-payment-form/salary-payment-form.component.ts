import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { SalaryPaymentService } from '../../services/salary-payment.service';
import {
  IEmployeeForSalary,
  ISalaryPaymentList,
} from '../../models/salary-payment.interface';
import {
  NgxDatatableModule,
  DatatableComponent,
} from '@swimlane/ngx-datatable';
import { PaginationQuery } from '@core/models/pagination-query';
import { DefaultPagination } from '@config/pagination';
import { PaymentMethod } from 'app/transaction/models/transaction.interface';

@Component({
  selector: 'app-salary-payment-form',
  templateUrl: './salary-payment-form.component.html',
  styleUrls: [],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgSelectModule,
    NgxDatatableModule,
    DatePipe,
    DecimalPipe,
  ],
})
export class SalaryPaymentFormComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  salaryForm: UntypedFormGroup;
  employees: IEmployeeForSalary[] = [];
  selectedEmployee: IEmployeeForSalary | null = null;
  isSubmitting = false;

  // Payment list properties
  paymentList: ISalaryPaymentList[] = [];
  loadingPayments = false;
  scrollBarHorizontal = window.innerWidth < 1200;
  pagination: PaginationQuery = {
    pageSize: 20,
    pageIndex: DefaultPagination.PAGEINDEX,
    orderBy: DefaultPagination.ORDERBY,
    isAscending: DefaultPagination.ASCENDING,
  };

  months = [
    { value: 1, text: 'January' },
    { value: 2, text: 'February' },
    { value: 3, text: 'March' },
    { value: 4, text: 'April' },
    { value: 5, text: 'May' },
    { value: 6, text: 'June' },
    { value: 7, text: 'July' },
    { value: 8, text: 'August' },
    { value: 9, text: 'September' },
    { value: 10, text: 'October' },
    { value: 11, text: 'November' },
    { value: 12, text: 'December' },
  ];

  years: number[] = [];

  paymentMethods = [
    { value: 'Cash', text: 'Cash' },
    { value: 'Bank Transfer', text: 'Bank Transfer' },
    { value: 'Cheque', text: 'Cheque' },
    { value: 'Mobile Banking', text: 'Mobile Banking' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private salaryPaymentService: SalaryPaymentService,
    private router: Router,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();

    window.onresize = () => {
      this.scrollBarHorizontal = window.innerWidth < 1200;
    };

    const currentYear = new Date().getFullYear();
    for (let i = currentYear - 2; i <= currentYear + 1; i++) {
      this.years.push(i);
    }

    this.salaryForm = this.fb.group({
      employeeId: [null, Validators.required],
      month: [new Date().getMonth() + 1, Validators.required],
      year: [currentYear, Validators.required],
      basicSalary: [null, [Validators.required, Validators.min(0)]],
      bonus: [null],
      deduction: [null],
      netAmount: [{ value: 0, disabled: true }],
      paymentMethod: [PaymentMethod.CASH, Validators.required],
      note: [''],
    });

    this.salaryForm.get('basicSalary')?.valueChanges.subscribe(() => {
      this.calculateNetAmount();
    });
    this.salaryForm.get('bonus')?.valueChanges.subscribe(() => {
      this.calculateNetAmount();
    });
    this.salaryForm.get('deduction')?.valueChanges.subscribe(() => {
      this.calculateNetAmount();
    });
  }

  ngOnInit(): void {
    this.loadEmployees();
    this.loadCurrentMonthPayments();
  }

  loadEmployees(): void {
    this.salaryPaymentService.getEmployeesForPayment().subscribe({
      next: (data: IEmployeeForSalary[]) => {
        this.employees = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load employees', 'Error');
        console.error('Error loading employees:', error);
      },
    });
  }

  onEmployeeChange(event: any): void {
    const employee = event;
    if (employee) {
      this.selectedEmployee = employee;
      this.salaryForm.patchValue({
        basicSalary: employee.salary,
      });
      this.calculateNetAmount();
    } else {
      this.selectedEmployee = null;
      this.reset();
    }
  }

  calculateNetAmount(): void {
    const basicSalary = this.salaryForm.get('basicSalary')?.value || 0;
    const bonus = this.salaryForm.get('bonus')?.value || 0;
    const deduction = this.salaryForm.get('deduction')?.value || 0;
    const netAmount = basicSalary + bonus - deduction;
    this.salaryForm.patchValue({ netAmount }, { emitEvent: false });
  }

  loadCurrentMonthPayments(): void {
    this.loadingPayments = true;
    const now = new Date();
    const currentMonth = now.getMonth() + 1;
    const currentYear = now.getFullYear();

    this.salaryPaymentService
      .getWithPagination(this.pagination, undefined, currentMonth, currentYear)
      .subscribe({
        next: (result) => {
          this.paymentList = result.data;
          this.loadingPayments = false;
        },
        error: (err) => {
          console.error('Error loading payments:', err);
          this.loadingPayments = false;
        },
      });
  }

  getMonthName(period: string): string {
    // Period format is "MM/YYYY"
    const parts = period.split('/');
    if (parts.length !== 2) return period;

    const monthNum = parseInt(parts[0], 10);
    const year = parts[1];
    const monthName =
      this.months.find((m) => m.value === monthNum)?.text || parts[0];

    return `${monthName} ${year}`;
  }

  onSubmit(): void {
    if (this.salaryForm.invalid) {
      this.toastr.error(
        'Please fill in all required fields',
        'Validation Error'
      );
      return;
    }

    this.isSubmitting = true;
    const formValue = this.salaryForm.getRawValue();

    const request = {
      employeeId: formValue.employeeId,
      month: formValue.month,
      year: formValue.year,
      basicSalary: formValue.basicSalary,
      bonus: formValue.bonus ?? 0,
      deduction: formValue.deduction ?? 0,
      paymentMethod: formValue.paymentMethod,
      note: formValue.note,
    };

    this.salaryPaymentService.createSalaryPayment(request).subscribe({
      next: () => {
        this.toastr.success('Salary payment created successfully', 'Success');
        this.loadCurrentMonthPayments();
        this.reset();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.toastr.error('Failed to create salary payment', 'Error');
        console.error('Error creating salary payment:', error);
        this.isSubmitting = false;
      },
    });
  }

  reset(): void {
    this.salaryForm.reset({
      month: new Date().getMonth() + 1,
      year: new Date().getFullYear(),
      bonus: 0,
      deduction: 0,
      paymentMethod: 'Cash',
    });
    this.selectedEmployee = null;
  }
}
