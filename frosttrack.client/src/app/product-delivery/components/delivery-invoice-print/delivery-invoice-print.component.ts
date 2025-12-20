import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LayoutService } from '@core/service/layout.service';
import {
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { ILookup } from '@core/models/lookup';
import { Subject } from 'rxjs';
import { NgSelectModule } from '@ng-select/ng-select';
import { DeliveryService } from '../../../delivery/services/delivery.service';
import { DeliveryInvoiceComponent } from '../delivery-invoice/delivery-invoice.component';

@Component({
  selector: 'app-delivery-invoice-print',
  templateUrl: './delivery-invoice-print.component.html',
  standalone: true,
  imports: [
    CommonModule,
    NgSelectModule,
    ReactiveFormsModule,
    DeliveryInvoiceComponent,
  ],
})
export class DeliveryInvoicePrintComponent implements OnInit {
  @ViewChild(DeliveryInvoiceComponent)
  invoiceComponent!: DeliveryInvoiceComponent;

  deliveryId: string = '';
  isDeliveryLoading: boolean = false;
  criteriaForm: UntypedFormGroup = this.fb.group({
    deliveryId: [null, [Validators.required]],
  });
  deliveryList: ILookup<string>[] = [];
  private deliveryListSubject: Subject<string> = new Subject<string>();

  constructor(
    private route: ActivatedRoute,
    private fb: UntypedFormBuilder,
    private router: Router,
    private deliveryService: DeliveryService,
    private toastr: ToastrService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    this.fetchDeliveryLookup();
    this.deliveryListSubject.subscribe((value: string) => {
      this.criteriaForm
        .get('deliveryId')
        ?.setValue(this.deliveryList.find((x) => x.value == value));
    });

    // Check if delivery ID is passed via route params
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.deliveryId = id;
    }
  }

  fetchDeliveryLookup() {
    this.isDeliveryLoading = true;
    this.deliveryService.getDeliveryLookup().subscribe({
      next: (response: ILookup<string>[]) => {
        this.deliveryList = response;
        this.isDeliveryLoading = false;
      },
      error: () => {
        this.isDeliveryLoading = false;
      },
    });
  }

  getDeliveryData() {
    const selectedDelivery = this.criteriaForm.get('deliveryId')?.value;
    if (selectedDelivery) {
      this.deliveryId = selectedDelivery;
    }
  }

  printInvoice(): void {
    if (this.invoiceComponent) {
      this.invoiceComponent.triggerPrint();
    }
  }

  goBack(): void {
    this.router.navigate(['/product-delivery/list']);
  }

  downloadPDF(): void {
    this.toastr.info('PDF download functionality will be implemented soon');
  }
}
