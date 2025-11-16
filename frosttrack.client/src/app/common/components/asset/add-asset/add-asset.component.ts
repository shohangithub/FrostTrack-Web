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
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import {
  Observable,
  OperatorFunction,
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
} from 'rxjs';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { AssetService } from '../../../services/asset.service';
import { IAssetRequest, IAssetResponse } from '../../../models/asset.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';

@Component({
  selector: 'app-add-asset',
  templateUrl: './add-asset.component.html',
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
  providers: [AssetService],
})
export class AddAssetComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  conditionList = [
    { value: 'Excellent', text: 'Excellent' },
    { value: 'Good', text: 'Good' },
    { value: 'Fair', text: 'Fair' },
    { value: 'Poor', text: 'Poor' },
  ];

  // Dynamic asset types from server
  availableAssetTypes: string[] = [];

  // Default asset types as fallback
  defaultAssetTypes = [
    'Computer',
    'Furniture',
    'Electronics',
    'Equipment',
    'Vehicle',
    'Software',
    'Other',
  ];

  private generatedCode: string = '';
  isGeneratingCode: boolean = false;

  // Autocomplete search function
  search: OperatorFunction<string, readonly string[]> = (
    text$: Observable<string>
  ) =>
    text$.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      filter((term) => term.length >= 1),
      map((term) => this.getFilteredAssetTypes(term))
    );

  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private assetService: AssetService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      assetName: new UntypedFormControl(),
      assetCode: new UntypedFormControl(),
      assetType: new UntypedFormControl(),
      serialNumber: new UntypedFormControl(),
      model: new UntypedFormControl(),
      manufacturer: new UntypedFormControl(),
      purchaseDate: new UntypedFormControl(),
      purchaseCost: new UntypedFormControl(),
      currentValue: new UntypedFormControl(),
      depreciationRate: new UntypedFormControl(),
      location: new UntypedFormControl(),
      department: new UntypedFormControl(),
      assignedTo: new UntypedFormControl(),
      condition: new UntypedFormControl(),
      warrantyExpiryDate: new UntypedFormControl(),
      maintenanceDate: new UntypedFormControl(),
      notes: new UntypedFormControl(),
      imageUrl: new UntypedFormControl(),
      isActive: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    this.loadAssetTypes();
    if (this.isEditing) {
      this.getExistingData();
    } else {
      this.generateCode();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      assetName: [null, [Validators.required]],
      assetCode: [null, [Validators.required]],
      assetType: [null],
      serialNumber: [null],
      model: [null],
      manufacturer: [null],
      purchaseDate: [null],
      purchaseCost: [0, [Validators.required, Validators.min(0)]],
      currentValue: [0, [Validators.required, Validators.min(0)]],
      depreciationRate: [0, [Validators.min(0), Validators.max(100)]],
      location: [null],
      department: [null],
      assignedTo: [null],
      condition: ['Good'],
      warrantyExpiryDate: [null],
      maintenanceDate: [null],
      notes: [null],
      imageUrl: [null],
      isActive: [true, [Validators.required]],
    });
  }

  generateCode() {
    this.isGeneratingCode = true;
    this.assetService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.patchValue({
          assetCode: this.generatedCode,
        });
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
      },
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.assetService.getById(this.row.id).subscribe({
      next: (response: IAssetResponse) => {
        this.register.patchValue({
          assetName: response.assetName,
          assetCode: response.assetCode,
          assetType: response.assetType,
          serialNumber: response.serialNumber,
          model: response.model,
          manufacturer: response.manufacturer,
          purchaseDate: response.purchaseDate
            ? new Date(response.purchaseDate).toISOString().split('T')[0]
            : null,
          purchaseCost: response.purchaseCost,
          currentValue: response.currentValue,
          depreciationRate: response.depreciationRate,
          location: response.location,
          department: response.department,
          assignedTo: response.assignedTo,
          condition: response.condition,
          warrantyExpiryDate: response.warrantyExpiryDate
            ? new Date(response.warrantyExpiryDate).toISOString().split('T')[0]
            : null,
          maintenanceDate: response.maintenanceDate
            ? new Date(response.maintenanceDate).toISOString().split('T')[0]
            : null,
          notes: response.notes,
          imageUrl: response.imageUrl,
          isActive: response.isActive,
        });
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  submit() {
    this.isSubmitted = true;
    if (this.register.invalid) {
      return;
    }

    this.isLoading = true;
    const formData = this.register.value;

    // Convert date strings to Date objects
    if (formData.purchaseDate) {
      formData.purchaseDate = new Date(formData.purchaseDate);
    }
    if (formData.warrantyExpiryDate) {
      formData.warrantyExpiryDate = new Date(formData.warrantyExpiryDate);
    }
    if (formData.maintenanceDate) {
      formData.maintenanceDate = new Date(formData.maintenanceDate);
    }

    const payload: IAssetRequest = {
      id: this.isEditing ? this.row.id : 0,
      ...formData,
    };

    if (this.isEditing) {
      this.assetService.update(this.row.id, payload).subscribe({
        next: (response: IAssetResponse) => {
          this.isLoading = false;
          this.activeModal.close({ success: true, data: response });
        },
        error: () => {
          this.isLoading = false;
        },
      });
    } else {
      this.assetService.create(payload).subscribe({
        next: (response: IAssetResponse) => {
          this.isLoading = false;
          this.activeModal.close({ success: true, data: response });
        },
        error: () => {
          this.isLoading = false;
        },
      });
    }
  }

  get f() {
    return this.register.controls;
  }

  closeModal() {
    this.activeModal.dismiss();
  }

  // Load asset types from server
  loadAssetTypes(): void {
    this.assetService.getDistinctAssetTypes().subscribe({
      next: (types: string[]) => {
        // Combine server types with default types and remove duplicates
        const allTypes = [...new Set([...types, ...this.defaultAssetTypes])];
        this.availableAssetTypes = allTypes.sort();
      },
      error: (err: ErrorResponse) => {
        console.warn(
          'Failed to load asset types from server, using defaults:',
          err
        );
        this.availableAssetTypes = [...this.defaultAssetTypes];
      },
    });
  }

  // Filter asset types for autocomplete
  getFilteredAssetTypes(term: string): string[] {
    const filtered = this.availableAssetTypes.filter((type) =>
      type.toLowerCase().includes(term.toLowerCase())
    );
    return filtered.slice(0, 10); // Limit to 10 suggestions
  }
}
