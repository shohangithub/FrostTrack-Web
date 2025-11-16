import { Component, Input, OnInit } from '@angular/core';
import { NgxDatatableModule } from '@swimlane/ngx-datatable';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  UntypedFormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbActiveModal, NgbTypeahead } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import {
  Observable,
  OperatorFunction,
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
} from 'rxjs';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { EmployeeService } from '../../../services/employee.service';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-add-employee',
  templateUrl: './add-employee.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
    NgbTypeahead,
  ],
  providers: [EmployeeService],
})
export class AddEmployeeComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  employmentTypeList = [
    { value: 'FullTime', text: 'Full Time' },
    { value: 'PartTime', text: 'Part Time' },
    { value: 'Contract', text: 'Contract' },
    { value: 'Intern', text: 'Intern' },
  ];

  // Dynamic departments and designations from server
  availableDepartments: string[] = [];
  availableDesignations: string[] = [];

  // Default departments as fallback
  defaultDepartments = [
    'Human Resources',
    'Finance',
    'IT',
    'Sales',
    'Marketing',
    'Operations',
    'Customer Service',
  ];

  // Default designations as fallback
  defaultDesignations = [
    'Manager',
    'Senior Executive',
    'Executive',
    'Assistant',
    'Analyst',
    'Specialist',
    'Coordinator',
  ];

  private generatedCode: string = '';
  isGeneratingCode: boolean = false;

  // Autocomplete search functions
  searchDepartments: OperatorFunction<string, readonly string[]> = (
    text$: Observable<string>
  ) =>
    text$.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      filter((term) => term.length >= 1),
      map((term) => this.getFilteredDepartments(term))
    );

  searchDesignations: OperatorFunction<string, readonly string[]> = (
    text$: Observable<string>
  ) =>
    text$.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      filter((term) => term.length >= 1),
      map((term) => this.getFilteredDesignations(term))
    );

  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private employeeService: EmployeeService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      employeeCode: new UntypedFormControl(),
      employeeName: new UntypedFormControl(),
      email: new UntypedFormControl(),
      phone: new UntypedFormControl(),
      dateOfBirth: new UntypedFormControl(),
      address: new UntypedFormControl(),
      city: new UntypedFormControl(),
      state: new UntypedFormControl(),
      zipCode: new UntypedFormControl(),
      country: new UntypedFormControl(),
      joiningDate: new UntypedFormControl(),
      department: new UntypedFormControl(),
      designation: new UntypedFormControl(),
      managerId: new UntypedFormControl(),
      salary: new UntypedFormControl(),
      employmentType: new UntypedFormControl(),
      bankAccountNumber: new UntypedFormControl(),
      emergencyContactName: new UntypedFormControl(),
      emergencyContactPhone: new UntypedFormControl(),
      notes: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    this.loadDepartments();
    this.loadDesignations();
    if (this.isEditing) {
      this.getExistingData();
    } else {
      this.generateCode();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      employeeCode: [null, [Validators.required]],
      employeeName: [null, [Validators.required]],
      email: [null, [Validators.email]],
      phone: [null],
      dateOfBirth: [null],
      address: [null],
      city: [null],
      state: [null],
      zipCode: [null],
      country: [null],
      joiningDate: [null, [Validators.required]],
      department: [null],
      designation: [null],
      managerId: [null],
      salary: [0, [Validators.required, Validators.min(0)]],
      employmentType: ['FullTime', [Validators.required]],
      bankAccountNumber: [null],
      emergencyContactName: [null],
      emergencyContactPhone: [null],
      notes: [null],
      isActive: [true],
    });
  }

  getExistingData() {
    if (this.row) {
      this.register.patchValue({
        employeeCode: this.row.employeeCode,
        employeeName: this.row.employeeName,
        email: this.row.email,
        phone: this.row.phone,
        dateOfBirth: this.row.dateOfBirth
          ? new Date(this.row.dateOfBirth).toISOString().split('T')[0]
          : null,
        address: this.row.address,
        city: null, // Not in backend model yet
        state: null, // Not in backend model yet
        zipCode: null, // Not in backend model yet
        country: null, // Not in backend model yet
        joiningDate: this.row.joiningDate
          ? new Date(this.row.joiningDate).toISOString().split('T')[0]
          : null,
        department: this.row.department,
        designation: this.row.designation,
        managerId: null, // Not in backend model yet
        salary: this.row.salary,
        employmentType: 'FullTime', // Default since not in backend model yet
        bankAccountNumber: this.row.bankAccount,
        emergencyContactName: this.row.emergencyContact,
        emergencyContactPhone: null, // Not in backend model yet
        notes: this.row.notes,
        isActive: this.row.status === 'Active',
      });
    }
  }

  // Load departments from server
  loadDepartments() {
    this.employeeService.getDistinctDepartments().subscribe({
      next: (departments) => {
        this.availableDepartments = departments;
      },
      error: () => {
        // Use default departments if server call fails
        this.availableDepartments = this.defaultDepartments;
      },
    });
  }

  // Load designations from server
  loadDesignations() {
    this.employeeService.getDistinctDesignations().subscribe({
      next: (designations) => {
        this.availableDesignations = designations;
      },
      error: () => {
        // Use default designations if server call fails
        this.availableDesignations = this.defaultDesignations;
      },
    });
  }

  // Filter departments for autocomplete
  getFilteredDepartments(term: string): string[] {
    const allDepartments = [
      ...new Set([...this.availableDepartments, ...this.defaultDepartments]),
    ];
    return allDepartments
      .filter((department) =>
        department.toLowerCase().includes(term.toLowerCase())
      )
      .slice(0, 10);
  }

  // Filter designations for autocomplete
  getFilteredDesignations(term: string): string[] {
    const allDesignations = [
      ...new Set([...this.availableDesignations, ...this.defaultDesignations]),
    ];
    return allDesignations
      .filter((designation) =>
        designation.toLowerCase().includes(term.toLowerCase())
      )
      .slice(0, 10);
  }

  // Generate employee code
  generateCode() {
    this.isGeneratingCode = true;
    this.employeeService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.patchValue({ employeeCode: this.generatedCode });
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
      },
    });
  }

  onSubmit() {
    this.isSubmitted = true;
    if (this.register.valid) {
      this.isLoading = true;

      const formValue = this.register.value;

      // Map form fields to match backend DTO structure
      const requestData = {
        id: 0, // Will be ignored for create operations
        employeeName: formValue.employeeName,
        employeeCode: formValue.employeeCode,
        employmentType: formValue.employmentType,
        email: formValue.email,
        phone: formValue.phone,
        address: formValue.address,
        dateOfBirth: formValue.dateOfBirth,
        joiningDate: formValue.joiningDate,
        department: formValue.department,
        designation: formValue.designation,
        salary: formValue.salary,
        emergencyContact: formValue.emergencyContact,
        bloodGroup: undefined, // Not in form yet
        nationalId: undefined, // Not in form yet
        passportNumber: undefined, // Not in form yet
        bankAccount: formValue.bankAccountNumber,
        notes: formValue.notes,
        photoUrl: undefined, // Not in form yet
        isActive: formValue.isActive,
        branchId: undefined, // Will be set by backend from current user
      };

      if (this.isEditing && this.row?.id) {
        // Update existing employee
        requestData.id = this.row.id;
        this.employeeService.update(this.row.id, requestData).subscribe({
          next: () => {
            this.activeModal.close({ success: true });
            this.isLoading = false;
          },
          error: () => {
            this.isLoading = false;
          },
        });
      } else {
        // Create new employee
        this.employeeService.create(requestData).subscribe({
          next: () => {
            this.activeModal.close({ success: true });
            this.isLoading = false;
          },
          error: () => {
            this.isLoading = false;
          },
        });
      }
    } else {
      this.toastr.error('Please fill in all required fields correctly.');
    }
  }

  onClose() {
    this.activeModal.close();
  }

  get form() {
    return this.register.controls;
  }
}
