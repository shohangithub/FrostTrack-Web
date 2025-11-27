import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { IEmployeeForSalary } from '../../models/salary-payment.interface';

@Component({
  selector: 'app-salary-payment-form',
  templateUrl: './salary-payment-form.component.html',
  styleUrls: [],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgSelectModule],
})
export class SalaryPaymentFormComponent implements OnInit {
  salaryForm: UntypedFormGroup;
  employees: IEmployeeForSalary[] = [];
  selectedEmployee: IEmployeeForSalary | null = null;
  isSubmitting = false;

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

    const currentYear = new Date().getFullYear();
    for (let i = currentYear - 2; i <= currentYear + 1; i++) {
      this.years.push(i);
    }

    this.salaryForm = this.fb.group({
      employeeId: [null, Validators.required],
      month: [new Date().getMonth() + 1, Validators.required],
      year: [currentYear, Validators.required],
      basicSalary: [0, [Validators.required, Validators.min(0)]],
      bonus: [0, [Validators.min(0)]],
      deduction: [0, [Validators.min(0)]],
      netAmount: [{ value: 0, disabled: true }],
      paymentMethod: ['Cash', Validators.required],
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
    }
  }

  calculateNetAmount(): void {
    const basicSalary = this.salaryForm.get('basicSalary')?.value || 0;
    const bonus = this.salaryForm.get('bonus')?.value || 0;
    const deduction = this.salaryForm.get('deduction')?.value || 0;
    const netAmount = basicSalary + bonus - deduction;
    this.salaryForm.patchValue({ netAmount }, { emitEvent: false });
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
      bonus: formValue.bonus,
      deduction: formValue.deduction,
      paymentMethod: formValue.paymentMethod,
      note: formValue.note,
    };

    this.salaryPaymentService.createSalaryPayment(request).subscribe({
      next: () => {
        this.toastr.success('Salary payment created successfully', 'Success');
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
