import { Component, Input, OnInit } from '@angular/core';
import {
  UntypedFormGroup,
  UntypedFormBuilder,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrService, ToastrModule } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { FormShimmerComponent } from '../../../shared/form-shimmer.component';
import { CompanyService } from '../../services/company.service';
import {
  ICompanyRequest,
  ICompanyResponse,
} from '../../models/company.interface';
import {
  ErrorResponse,
  formatErrorMessage,
} from '../../../utils/server-error-handler';

@Component({
  selector: 'app-company-config',
  templateUrl: './company-config.component.html',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    FormShimmerComponent,
  ],
  providers: [CompanyService],
})
export class CompanyConfigComponent implements OnInit {
  @Input() isEditing = false;
  @Input() row: any = null;

  register!: UntypedFormGroup;
  isLoading = false;
  isSaveLoading = false;

  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private companyService: CompanyService
  ) {}

  ngOnInit(): void {
    this.initFormData();
    if (this.isEditing && this.row) {
      this.getExistingData();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      name: [null, [Validators.required]],
      logoUrl: [null],
      businessCurrency: [null],
      currencySymbol: [null],
      description: [null],
      autoInvoicePrint: [false],
      invoiceHeader: [null],
      invoiceFooter: [null],
      isSingleBranch: [false],
      codeGeneration: [2, [Validators.required]], // Default to Company
      isActive: [true, [Validators.required]],
    });
  }

  getExistingData() {
    this.isLoading = true;
    this.companyService.getById(this.row.id).subscribe({
      next: (response: ICompanyResponse) => {
        this.register.patchValue({
          name: response.name,
          logoUrl: response.logoUrl,
          businessCurrency: response.businessCurrency,
          currencySymbol: response.currencySymbol,
          description: response.description,
          autoInvoicePrint: response.autoInvoicePrint,
          invoiceHeader: response.invoiceHeader,
          invoiceFooter: response.invoiceFooter,
          isSingleBranch: response.isSingleBranch,
          codeGeneration: response.codeGeneration,
          isActive: response.isActive,
        });
        this.isLoading = false;
      },
      error: (err: ErrorResponse) => {
        this.isLoading = false;
        const errString = formatErrorMessage(err);
        this.toastr.error(errString);
      },
    });
  }

  onSubmit() {
    if (this.register.valid) {
      this.isSaveLoading = true;
      const payload: ICompanyRequest = {
        name: this.register.value.name,
        logoUrl: this.register.value.logoUrl,
        businessCurrency: this.register.value.businessCurrency,
        currencySymbol: this.register.value.currencySymbol,
        description: this.register.value.description,
        autoInvoicePrint: this.register.value.autoInvoicePrint,
        invoiceHeader: this.register.value.invoiceHeader,
        invoiceFooter: this.register.value.invoiceFooter,
        isSingleBranch: this.register.value.isSingleBranch,
        codeGeneration: this.register.value.codeGeneration,
        isActive: this.register.value.isActive,
      };

      if (this.isEditing) {
        this.companyService.update(this.row.id, payload).subscribe({
          next: () => {
            this.isSaveLoading = false;
            this.activeModal.close(true);
          },
          error: (err: ErrorResponse) => {
            this.isSaveLoading = false;
            const errString = formatErrorMessage(err);
            this.toastr.error(errString);
          },
        });
      } else {
        this.companyService.create(payload).subscribe({
          next: () => {
            this.isSaveLoading = false;
            this.activeModal.close(true);
          },
          error: (err: ErrorResponse) => {
            this.isSaveLoading = false;
            const errString = formatErrorMessage(err);
            this.toastr.error(errString);
          },
        });
      }
    }
  }
}
