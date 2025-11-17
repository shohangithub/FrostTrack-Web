import { CommonModule } from '@angular/common';
import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormControl,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { BarcodeComponent } from '@shared/barcode.component';
import { FormShimmerComponent } from '@shared/form-shimmer.component';
import { IProductListResponse } from 'app/administration/models/product.interface';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { NgxPrintModule } from 'ngx-print';
import { ISingleBarcode } from 'app/administration/models/single-barcode.interface';

@Component({
  selector: 'app-generate-single-barcode',
  templateUrl: './generate-single-barcode.component.html',
  styles: `
  @media print {
        .page-break {
            display: block;
            page-break-before: always;
        }

        .article {
            min-height: 65px;
            max-height: 100px;
            float: left !important;
            writing-mode: tb-rl;
            line-height: 0;
            font-weight: 700;
            transform: rotate(180deg);
        }

        .content {
            width: 120px;
            float: left !important;
            padding: 2px;
        }
    }
  `,
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    ReactiveFormsModule,
    ToastrModule,
    CommonModule,
    BarcodeComponent,
    FormShimmerComponent,
    NgxPrintModule,
  ],
})
export class GenerateSingleBarcodeComponent implements OnInit {
  @Input() data?: IProductListResponse;

  register!: UntypedFormGroup;
  isLoading = false;
  isSubmitted = false;

  @ViewChild('printBody', { static: true }) templateRef?: ElementRef;
  constructor(
    private fb: UntypedFormBuilder,
    public activeModal: NgbActiveModal,
    private toastr: ToastrService,
    private modalService: NgbModal
  ) {
    this.register = this.fb.group({
      id: new UntypedFormControl(),
      productName: new UntypedFormControl(),
      productCode: new UntypedFormControl(),
      customBarcode: new UntypedFormControl(),
      article: new UntypedFormControl(),
      width: new UntypedFormControl(),
      height: new UntypedFormControl(),
      bookingRate: new UntypedFormControl(),
      quantity: new UntypedFormControl(),
      isSingle: new UntypedFormControl(),
    });
  }

  ngOnInit(): void {
    if (this.data) {
      this.initFormData();
    }
  }

  initFormData() {
    this.register = this.fb.group({
      id: [this.data?.id, [Validators.required]],
      productCode: [this.data?.productName, [Validators.required]],
      productName: [this.data?.productCode, [Validators.required]],
      customBarcode: [this.data?.customBarcode],
      bookingRate: [this.data?.bookingRate, [Validators.required]],
      quantity: [null, [Validators.required]],
      article: [null],
      isSingle: [false],
      width: [1.5],
      height: [1],
    });
  }

  // edit a record
  isGenerated: boolean = false;
  barcodeModel?: ISingleBarcode;
  generate(form: UntypedFormGroup) {
    if (this.register.valid) {
      this.barcodeModel = { ...form.value };

      this.isGenerated = true;
    }
  }
}
