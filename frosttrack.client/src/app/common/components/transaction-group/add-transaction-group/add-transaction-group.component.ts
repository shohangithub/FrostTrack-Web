import { Component, Input, OnInit } from '@angular/core';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import {
  ErrorResponse,
  formatErrorMessage,
} from 'app/utils/server-error-handler';

import { COMMON_STATUS_LIST } from 'app/common/data/settings-data';
import { TransactionGroupService } from '../../../services/transaction-group.service';
import {
  ITransactionGroupRequest,
  ITransactionGroupResponse,
} from '../../../models/transaction-group.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';

@Component({
  selector: 'app-add-transaction-group',
  templateUrl: './add-transaction-group.component.html',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [TransactionGroupService],
})
export class AddTransactionGroupComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  form!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;

  constructor(
    private fb: UntypedFormBuilder,
    public modal: NgbActiveModal,
    private toastr: ToastrService,
    private transactionGroupService: TransactionGroupService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      sortOrder: [0],
      description: [''],
      isActive: ['true', [Validators.required]],
    });
    if (this.isEditing) {
      this.getExistingData();
    }
  }

  getExistingData() {
    this.isLoading = true;
    this.transactionGroupService.getById(this.row.id).subscribe({
      next: (response: ITransactionGroupResponse) => {
        this.form.setValue({
          name: response.name,
          sortOrder: response.sortOrder,
          description: response.description || '',
          isActive: String(response.isActive),
        });
        this.isLoading = false;
      },
      error: (err) => {
        this.toastr.error(formatErrorMessage(err));
        this.isLoading = false;
      },
    });
  }

  submit() {
    if (!this.form.valid) return;

    this.isSubmitted = true;
    const formData = this.form.value;
    const payload: ITransactionGroupRequest = {
      name: formData.name,
      sortOrder: Number(formData.sortOrder) || 0,
      description: formData.description,
      isActive: formData.isActive === true || formData.isActive === 'true',
    };

    const request$ = this.isEditing
      ? this.transactionGroupService.update(this.row.id, payload)
      : this.transactionGroupService.create(payload);

    request$.subscribe({
      next: (response: ITransactionGroupResponse) => {
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
