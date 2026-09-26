import { Component, Input, OnInit } from '@angular/core';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  UntypedFormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';
import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { UnitConversionService } from '../../../services/unit-conversion.service';
import { BaseUnitService } from '../../../services/base-unit.service';
import {
  IUnitConversionRequest,
  IUnitConversionResponse,
} from '../../../models/unit-conversion.interface';
import { ILookup } from 'app/core/models/lookup';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';

@Component({
  selector: 'app-add-unit-conversion',
  templateUrl: './add-unit-conversion.component.html',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [UnitConversionService, BaseUnitService],
})
export class AddUnitConversionComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  baseUnits: ILookup<number>[] = [];
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;

  constructor(
    private fb: UntypedFormBuilder,
    public modal: NgbActiveModal,
    private toastr: ToastrService,
    private unitConversionService: UnitConversionService,
    private baseUnitService: BaseUnitService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      unitName: new UntypedFormControl('', [Validators.required]),
      baseUnitId: new UntypedFormControl(null, [Validators.required]),
      conversionValue: new UntypedFormControl(null, [
        Validators.required,
        Validators.min(0.0001),
      ]),
      description: new UntypedFormControl(''),
      isActive: new UntypedFormControl(true, [Validators.required]),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    this.fetchBaseUnitLookup();
    if (this.isEditing) {
      this.getExistingData();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      unitName: ['', [Validators.required]],
      baseUnitId: [null, [Validators.required]],
      conversionValue: [null, [Validators.required, Validators.min(0.0001)]],
      description: [''],
      isActive: [true, [Validators.required]],
    });
  }

  fetchBaseUnitLookup() {
    this.baseUnitService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.baseUnits = response;
      },
      error: (err: ErrorResponse) => {
        this.toastr.error(formatErrorMessage(err));
      },
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.unitConversionService.getById(this.row.id).subscribe({
      next: (response: IUnitConversionResponse) => {
        this.editForm.setValue({
          id: response.id,
          unitName: response.unitName,
          baseUnitId: response.baseUnitId,
          conversionValue: response.conversionValue,
          description: response.description || '',
          isActive: response.isActive ?? true,
        });
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(formatErrorMessage(err));
        this.isLoading = false;
      },
    });
  }

  add(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const payload: IUnitConversionRequest = { ...form.value };
      this.unitConversionService.create(payload).subscribe({
        next: (response: IUnitConversionResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: (err: ErrorResponse) => {
          this.isSubmitted = false;
          this.toastr.error(formatErrorMessage(err));
        },
      });
    }
  }

  edit(form: UntypedFormGroup) {
    if (this.editForm.valid) {
      this.isSubmitted = true;
      const formData = form.value;
      const payload: IUnitConversionRequest = { ...formData };
      this.unitConversionService.update(formData.id, payload).subscribe({
        next: (response: IUnitConversionResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: (err: ErrorResponse) => {
          this.isSubmitted = false;
          this.toastr.error(formatErrorMessage(err));
        },
      });
    }
  }
}
