import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NgxPrintModule } from 'ngx-print';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import { EmployeeReportService } from '../../services/employee-report.service';
import { EmployeeService } from 'app/common/services/employee.service';
import { IEmployeeReportItem } from '../../models/employee-report.interface';
import { ReportInvoiceHeaderComponent } from '@shared/components/reports/report-invoice-header.component/report-invoice-header.component';
import { ReportFooterComponent } from '@shared/components/reports/report-footer.component/report-footer.component';

interface IStatusOption {
  value: boolean | null;
  label: string;
}

@Component({
  selector: 'app-employee-report',
  templateUrl: './employee-report.component.html',
  styleUrls: ['./employee-report.component.scss'],
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
export class EmployeeReportComponent implements OnInit {
  reportForm: UntypedFormGroup;
  employeeReportItems: IEmployeeReportItem[] = [];
  isLoading = false;
  showReport = false;

  departments: string[] = [];
  designations: string[] = [];

  statusOptions: IStatusOption[] = [
    { value: null, label: 'All' },
    { value: true, label: 'Active' },
    { value: false, label: 'Inactive' },
  ];

  employmentTypeList = [
    { value: 'FullTime', text: 'Full Time' },
    { value: 'PartTime', text: 'Part Time' },
    { value: 'Contract', text: 'Contract' },
    { value: 'Intern', text: 'Intern' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private employeeReportService: EmployeeReportService,
    private employeeService: EmployeeService,
    private toastr: ToastrService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();

    this.reportForm = this.fb.group({
      department: [null],
      designation: [null],
      employmentType: [null],
      isActive: [null],
    });
  }

  ngOnInit(): void {
    this.loadDepartments();
    this.loadDesignations();
  }

  loadDepartments(): void {
    this.employeeService.getDistinctDepartments().subscribe({
      next: (data: string[]) => {
        this.departments = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load departments', 'Error');
        console.error('Error loading departments:', error);
      },
    });
  }

  loadDesignations(): void {
    this.employeeService.getDistinctDesignations().subscribe({
      next: (data: string[]) => {
        this.designations = data;
      },
      error: (error: any) => {
        this.toastr.error('Failed to load designations', 'Error');
        console.error('Error loading designations:', error);
      },
    });
  }

  generateReport(): void {
    this.isLoading = true;
    this.showReport = false;

    const formValue = this.reportForm.value;

    this.employeeReportService
      .getEmployeeReport(
        formValue.department ?? undefined,
        formValue.designation ?? undefined,
        formValue.employmentType ?? undefined,
        formValue.isActive ?? undefined,
      )
      .subscribe({
        next: (data: IEmployeeReportItem[]) => {
          this.employeeReportItems = data;
          this.showReport = true;
          this.isLoading = false;

          if (data.length === 0) {
            this.toastr.info(
              'No employees found for the selected criteria',
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

  reset(): void {
    this.reportForm.reset({
      department: null,
      designation: null,
      employmentType: null,
      isActive: null,
    });
    this.showReport = false;
    this.employeeReportItems = [];
  }

  getTotalSalary(): number {
    return this.employeeReportItems.reduce((sum, item) => sum + item.salary, 0);
  }

  getActiveCount(): number {
    return this.employeeReportItems.filter((e) => e.status === 'Active').length;
  }

  getInactiveCount(): number {
    return this.employeeReportItems.filter((e) => e.status === 'Inactive')
      .length;
  }

  getSelectedDepartmentLabel(): string {
    return this.reportForm.value.department ?? 'All Departments';
  }

  getSelectedDesignationLabel(): string {
    return this.reportForm.value.designation ?? 'All Designations';
  }

  getSelectedEmploymentTypeLabel(): string {
    return this.reportForm.value.employmentType ?? 'All Types';
  }

  getSelectedStatusLabel(): string {
    const val = this.reportForm.value.isActive;
    if (val === true) return 'Active';
    if (val === false) return 'Inactive';
    return 'All';
  }
}
