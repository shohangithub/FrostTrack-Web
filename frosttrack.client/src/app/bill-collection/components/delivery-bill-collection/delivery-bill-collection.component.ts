import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  FormsModule,
} from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { Router, RouterLink } from '@angular/router';
import { TransactionService } from 'app/transaction/services/transaction.service';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import {
  BillCollectionService,
  IDeliveryBillCollectionRequest,
} from '../../services/bill-collection.service';
import { DeliveryService } from 'app/product-delivery/services/product-delivery.service';
import { IDeliveryResponse } from 'app/product-delivery/models/product-delivery.interface';
import { CustomerService } from 'app/common/services/customer.service';
import { ICustomerListResponse } from 'app/common/models/customer.interface';

@Component({
  selector: 'app-delivery-bill-collection',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    NgSelectModule,
    RouterLink,
  ],
  templateUrl: './delivery-bill-collection.component.html',
})
export class DeliveryBillCollectionComponent implements OnInit {
  billCollectionForm!: FormGroup;
  customers: ICustomerListResponse[] = [];
  deliveryCodes: Array<{ value: string; text: string; customerId: number }> =
    [];
  unpaidDeliveries: IDeliveryResponse[] = [];
  selectedDeliveries = new Set<string>();
  deliveryLoading = false;
  customerLoading = false;
  deliveryCodesLoading = false;
  isLoading = false;
  isSubmitting = false;
  isGeneratingCode = false;
  transactionCode = '';
  selectedBranch!: number;
  searchMode: 'customer' | 'code' = 'customer';
  selectedDeliveryCode: string | null = null;

  paymentMethods = [
    { value: 'CASH', label: 'Cash' },
    { value: 'BANK_TRANSFER', label: 'Bank Transfer' },
    { value: 'CHEQUE', label: 'Cheque' },
    { value: 'MOBILE_BANKING', label: 'Mobile Banking' },
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private deliveryService: DeliveryService,
    private customerService: CustomerService,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private router: Router,
    private authService: AuthService,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();
    this.loadCustomers();
    this.generateTransactionCode();
  }

  initForm() {
    this.billCollectionForm = this.fb.group({
      transactionCode: ['', Validators.required],
      transactionDate: [
        new Date().toISOString().split('T')[0],
        Validators.required,
      ],
      customerId: [null],
      branchId: [this.selectedBranch, Validators.required],
      amount: [{ value: 0, disabled: true }],
      paymentMethod: ['CASH', Validators.required],
      paymentReference: [''],
      note: [''],
    });

    this.billCollectionForm
      .get('customerId')
      ?.valueChanges.subscribe((customerId) => {
        if (customerId && this.searchMode === 'customer') {
          this.loadUnpaidDeliveriesByCustomer(customerId);
        } else if (!customerId && this.searchMode === 'customer') {
          // Clear deliveries when customer is cleared
          this.unpaidDeliveries = [];
          this.selectedDeliveries.clear();
          this.billCollectionForm.patchValue({ amount: 0 });
        }
      });
  }

  loadCustomers() {
    this.customerLoading = true;
    this.customerService.getList().subscribe({
      next: (response) => {
        this.customers = response;
        this.customerLoading = false;
      },
      error: (err) => {
        console.error('Failed to load customers:', err);
        this.toastr.error('Failed to load customers');
        this.customerLoading = false;
      },
    });
  }

  loadAllUnpaidDeliveryCodes() {
    this.deliveryCodesLoading = true;

    // Single optimized API call to get all unpaid deliveries
    this.deliveryService.getAllUnpaidDeliveries().subscribe({
      next: (deliveries) => {
        this.deliveryCodes = deliveries.map((d) => ({
          value: d.deliveryNumber,
          text: `${d.deliveryNumber} - ${d.customerName}`,
          customerId: d.customerId,
        }));
        this.deliveryCodesLoading = false;
      },
      error: (err) => {
        console.error('Failed to load delivery codes:', err);
        this.toastr.error('Failed to load delivery codes');
        this.deliveryCodesLoading = false;
      },
    });
  }

  loadUnpaidDeliveriesByCustomer(customerId: number) {
    this.deliveryLoading = true;
    this.unpaidDeliveries = [];
    this.selectedDeliveries.clear();

    this.deliveryService.getUnpaidDeliveriesByCustomer(customerId).subscribe({
      next: (deliveries) => {
        this.unpaidDeliveries = deliveries;
        // Select all deliveries by default
        deliveries.forEach((delivery) =>
          this.selectedDeliveries.add(delivery.id)
        );
        this.updateTotalAmount();
        this.deliveryLoading = false;
      },
      error: (err) => {
        console.error('Failed to load unpaid deliveries:', err);
        this.toastr.error('Failed to load unpaid deliveries');
        this.deliveryLoading = false;
      },
    });
  }

  onDeliveryCodeSelect(deliveryCode: string) {
    if (!deliveryCode) {
      this.unpaidDeliveries = [];
      this.selectedDeliveries.clear();
      return;
    }

    this.deliveryLoading = true;
    this.unpaidDeliveries = [];
    this.selectedDeliveries.clear();

    this.deliveryService.getUnpaidDeliveryByCode(deliveryCode).subscribe({
      next: (delivery) => {
        this.unpaidDeliveries = [delivery];
        this.selectedDeliveries.add(delivery.id);
        // Auto-fill customer
        if (delivery.customerId) {
          this.billCollectionForm.patchValue({
            customerId: delivery.customerId,
          });
        }
        this.updateTotalAmount();
        this.deliveryLoading = false;
      },
      error: (err) => {
        console.error('Delivery not found:', err);
        this.toastr.error('Unpaid delivery not found with this code');
        this.deliveryLoading = false;
      },
    });
  }

  switchSearchMode(mode: 'customer' | 'code') {
    this.searchMode = mode;
    this.unpaidDeliveries = [];
    this.selectedDeliveries.clear();
    this.selectedDeliveryCode = null;
    this.billCollectionForm.patchValue({ customerId: null, amount: 0 });

    // Load delivery codes when switching to code mode
    if (mode === 'code' && this.deliveryCodes.length === 0) {
      this.loadAllUnpaidDeliveryCodes();
    }
  }

  toggleDeliverySelection(deliveryId: string) {
    if (this.selectedDeliveries.has(deliveryId)) {
      this.selectedDeliveries.delete(deliveryId);
    } else {
      this.selectedDeliveries.add(deliveryId);
    }
    this.updateTotalAmount();
  }

  isDeliverySelected(deliveryId: string): boolean {
    return this.selectedDeliveries.has(deliveryId);
  }

  updateTotalAmount() {
    const total = this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => sum + d.chargeAmount + d.adjustmentValue, 0);

    this.billCollectionForm.patchValue({ amount: total });
  }

  toggleSelectAll() {
    if (this.selectedCount === this.unpaidDeliveries.length) {
      // Unselect all
      this.selectedDeliveries.clear();
    } else {
      // Select all
      this.unpaidDeliveries.forEach((d) => this.selectedDeliveries.add(d.id));
    }
    this.updateTotalAmount();
  }

  get selectedTotal(): number {
    return this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => sum + d.chargeAmount + d.adjustmentValue, 0);
  }

  get selectedCount(): number {
    return this.selectedDeliveries.size;
  }

  generateTransactionCode() {
    this.isGeneratingCode = true;
    this.transactionService.generateCode().subscribe({
      next: (response: any) => {
        this.transactionCode = response.code;
        this.billCollectionForm.patchValue({
          transactionCode: this.transactionCode,
        });
        this.isGeneratingCode = false;
      },
      error: (err: any) => {
        console.error('Failed to generate transaction code:', err);
        this.toastr.error('Failed to generate transaction code');
        this.isGeneratingCode = false;
      },
    });
  }

  onSubmit() {
    if (this.billCollectionForm.invalid) {
      this.billCollectionForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields');
      return;
    }

    if (this.selectedDeliveries.size === 0) {
      this.toastr.error('Please select at least one delivery');
      return;
    }

    const formValue = this.billCollectionForm.getRawValue();

    if (formValue.amount <= 0) {
      this.toastr.error('Amount must be greater than 0');
      return;
    }

    this.isSubmitting = true;

    const payload: IDeliveryBillCollectionRequest = {
      transactionCode: formValue.transactionCode,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      deliveryIds: Array.from(this.selectedDeliveries),
      amount: formValue.amount,
      paymentMethod: formValue.paymentMethod,
      paymentReference: formValue.paymentReference,
      note: formValue.note,
    };

    this.billCollectionService.createDeliveryBillCollection(payload).subscribe({
      next: () => {
        this.router.navigate(['/bill-collection/list']);
      },
      error: () => {
        this.isSubmitting = false;
      },
    });
  }

  cancel() {
    this.router.navigate(['/bill-collection/list']);
  }

  reset() {
    this.unpaidDeliveries = [];
    this.selectedDeliveries.clear();
    this.selectedDeliveryCode = null;
    this.initForm();
    this.generateTransactionCode();
  }
}
