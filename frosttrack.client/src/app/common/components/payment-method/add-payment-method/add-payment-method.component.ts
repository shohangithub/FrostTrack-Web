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
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule } from 'ngx-toastr';
import { CommonModule } from '@angular/common';

import {
  COMMON_STATUS_LIST,
  DEFAULT_PAYMENT_METHOD_TYPE,
  PAYMENT_METHOD_ICONS,
} from 'app/common/data/settings-data';
import { PaymentMethodService } from '../../../services/payment-method.service';
import {
  IPaymentMethodRequest,
  IPaymentMethodResponse,
} from '../../../models/payment-method.interface';
import { FormShimmerComponent } from '../../../../shared/form-shimmer.component';
import { CodeResponse } from '@core/models/code-response';
import { NgSelectModule } from '@ng-select/ng-select';

@Component({
  selector: 'app-add-payment-method',
  templateUrl: './add-payment-method.component.html',
  standalone: true,
  imports: [
    NgxDatatableModule,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
    NgSelectModule,
  ],
  providers: [PaymentMethodService],
})
export class AddPaymentMethodComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  editForm: UntypedFormGroup;
  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;
  statusList = COMMON_STATUS_LIST;
  categories = DEFAULT_PAYMENT_METHOD_TYPE;
  iconOptions = PAYMENT_METHOD_ICONS;
  private generatedCode: string = '';
  isGeneratingCode: boolean = false;

  constructor(
    private fb: UntypedFormBuilder,
    public modal: NgbActiveModal,
    private paymentMethodService: PaymentMethodService
  ) {
    this.editForm = this.fb.group({
      id: new UntypedFormControl(),
      methodName: new UntypedFormControl(),
      code: new UntypedFormControl(),
      description: new UntypedFormControl(),
      category: new UntypedFormControl(),
      requiresBankAccount: new UntypedFormControl(false),
      requiresCheckDetails: new UntypedFormControl(false),
      requiresOnlineDetails: new UntypedFormControl(false),
      requiresMobileWalletDetails: new UntypedFormControl(false),
      requiresCardDetails: new UntypedFormControl(false),
      isActive: new UntypedFormControl(),
      sortOrder: new UntypedFormControl(0),
      iconClass: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    this.initFormData();
    if (this.isEditing) {
      this.getExistingData();
    } else {
      this.generateCode();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      methodName: ['', [Validators.required]],
      code: ['', [Validators.required]],
      description: [''],
      category: ['', [Validators.required]],
      requiresBankAccount: [false],
      requiresCheckDetails: [false],
      requiresOnlineDetails: [false],
      requiresMobileWalletDetails: [false],
      requiresCardDetails: [false],
      isActive: [true, [Validators.required]],
      sortOrder: [0],
      iconClass: [''],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.paymentMethodService.getById(this.row.id).subscribe({
      next: (response: IPaymentMethodResponse) => {
        this.editForm.setValue({
          id: response.id,
          methodName: response.methodName,
          code: response.code,
          description: response.description,
          category: response.category,
          requiresBankAccount: response.requiresBankAccount,
          requiresCheckDetails: response.requiresCheckDetails,
          requiresOnlineDetails: response.requiresOnlineDetails,
          requiresMobileWalletDetails: response.requiresMobileWalletDetails,
          requiresCardDetails: response.requiresCardDetails,
          isActive: response.isActive || false,
          sortOrder: response.sortOrder || 0,
          iconClass: response.iconClass || '',
        });
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        // BaseService already handles error toasts via ErrorHandlerService
      },
    });
  }

  getCategoryText(value: string): string {
    const category = this.categories.find((c) => c.value === value);
    return category ? category.text : value;
  }

  // generateCode() {
  //   this.isGeneratingCode = true;
  //   this.paymentMethodService.generateCode().subscribe({
  //     next: (code: string) => {
  //       this.register.patchValue({ code: code });
  //       this.isGeneratingCode = false;
  //     },
  //     error: (error: any) => {
  //       this.isGeneratingCode = false;
  //       console.error('Error generating code:', error);
  //     },
  //   });
  // }
  generateCode() {
    this.isGeneratingCode = true;
    this.paymentMethodService.generateCode().subscribe({
      next: (response: CodeResponse) => {
        this.generatedCode = response.code;
        this.register.get('code')?.setValue(this.generatedCode);
        this.isGeneratingCode = false;
      },
      error: () => {
        this.isGeneratingCode = false;
        // BaseService already handles error toasts via ErrorHandlerService
      },
    });
  }

  // add new record
  add(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.isSubmitted = true;
      const payload: IPaymentMethodRequest = { ...form.value };
      this.paymentMethodService.create(payload).subscribe({
        next: (response: IPaymentMethodResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: () => {
          this.isSubmitted = false;
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }
  // edit a record
  edit(form: UntypedFormGroup) {
    if (this.editForm.valid) {
      this.isSubmitted = true;
      const formData = form.value;
      const payload: IPaymentMethodRequest = { ...formData };
      this.paymentMethodService.update(formData.id, payload).subscribe({
        next: (response: IPaymentMethodResponse) => {
          this.isSubmitted = false;
          this.modal.close({ success: true, data: response });
        },
        error: () => {
          this.isSubmitted = false;
          // BaseService already handles error toasts via ErrorHandlerService
        },
      });
    }
  }
}
