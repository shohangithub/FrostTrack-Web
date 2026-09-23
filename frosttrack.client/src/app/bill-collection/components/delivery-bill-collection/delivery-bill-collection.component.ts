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
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
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
import { BankService } from 'app/common/services/bank.service';
import { BookingService } from 'app/booking/services/booking.service';
import { ICustomerDueDetailResponse } from 'app/booking/models/booking.interface';
import { ILookup } from '@core/models/lookup';

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
  customerBookings: ICustomerDueDetailResponse[] = [];
  banks: ILookup<number>[] = [];
  deliveryCodes: Array<{ value: string; text: string; customerId: number }> = [];
  unpaidDeliveries: IDeliveryResponse[] = [];
  selectedDeliveries = new Set<string>();
  deliveryLoading = false;
  customerLoading = false;
  customerBookingsLoading = false;
  deliveryCodesLoading = false;
  isLoading = false;
  isSubmitting = false;
  isGeneratingCode = false;
  isEditing = false;
  saveAndPrint = false;
  transactionCode = '';
  selectedBranch!: number;
  searchMode: 'customer' | 'code' | 'advance' = 'customer';
  selectedDeliveryCode: string | null = null;

  paymentMethods = [
    { value: 'CASH', label: 'Cash (নগদ)' },
    { value: 'BANK_TRANSFER', label: 'Bank Transfer (ব্যাংক ট্রান্সফার)' },
    { value: 'CHEQUE', label: 'Bank Cheque (চেক)' },
  ];

  constructor(
    private fb: FormBuilder,
    private billCollectionService: BillCollectionService,
    private deliveryService: DeliveryService,
    private customerService: CustomerService,
    private bookingService: BookingService,
    private bankService: BankService,
    private transactionService: TransactionService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private layoutService: LayoutService,
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit() {
    this.selectedBranch = this.authService.currentBranchId;
    this.initForm();
    this.loadCustomers();
    this.loadBanks();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditing = true;
      this.loadExistingTransaction(id);
    } else {
      this.generateTransactionCode();
    }
  }

  loadBanks() {
    this.bankService.getLookup().subscribe({
      next: (res) => (this.banks = res),
      error: (err) => console.error('Failed to load banks:', err),
    });
  }

  initForm() {
    this.billCollectionForm = this.fb.group({
      id: ['00000000-0000-0000-0000-000000000000'],
      transactionCode: ['', Validators.required],
      transactionDate: [
        new Date().toISOString().split('T')[0],
        Validators.required,
      ],
      customerId: [null],
      bookingId: [null],
      branchId: [this.selectedBranch, Validators.required],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      paymentMethod: ['CASH', Validators.required],
      bankId: [null],
      paymentReference: [''],
      note: [''],
    });

    this.billCollectionForm
      .get('customerId')
      ?.valueChanges.subscribe((customerId) => {
        this.customerBookings = [];
        this.billCollectionForm.patchValue(
          { bookingId: null },
          { emitEvent: false }
        );
        if (customerId) {
          this.loadCustomerBookings(customerId);
          if (this.searchMode === 'customer') {
            this.loadUnpaidDeliveriesByCustomer(customerId);
          }
        } else {
          this.unpaidDeliveries = [];
          this.selectedDeliveries.clear();
          if (this.searchMode !== 'advance') {
            this.billCollectionForm.patchValue({ amount: 0 });
          }
        }
      });

    this.billCollectionForm
      .get('paymentMethod')
      ?.valueChanges.subscribe((pm) => {
        const bankControl = this.billCollectionForm.get('bankId');
        if (pm === 'BANK_TRANSFER' || pm === 'CHEQUE') {
          bankControl?.setValidators([Validators.required]);
        } else {
          bankControl?.clearValidators();
          bankControl?.setValue(null);
        }
        bankControl?.updateValueAndValidity();
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

  loadCustomerBookings(customerId: number) {
    this.customerBookingsLoading = true;
    this.bookingService.getCustomerDueDetail(customerId).subscribe({
      next: (bookings) => {
        this.customerBookings = bookings || [];
        this.customerBookingsLoading = false;
      },
      error: (err) => {
        console.error('Failed to load customer bookings:', err);
        this.customerBookingsLoading = false;
      },
    });
  }

  loadAllUnpaidDeliveryCodes() {
    this.deliveryCodesLoading = true;
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
        deliveries.forEach((delivery) =>
          this.selectedDeliveries.add(delivery.id),
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
        if (delivery.customerId) {
          this.billCollectionForm.patchValue({
            customerId: delivery.customerId,
          });
          this.loadCustomerBookings(delivery.customerId);
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

  switchSearchMode(mode: 'customer' | 'code' | 'advance') {
    this.searchMode = mode;
    this.unpaidDeliveries = [];
    this.selectedDeliveries.clear();
    this.selectedDeliveryCode = null;
    this.customerBookings = [];
    this.billCollectionForm.patchValue({
      customerId: null,
      bookingId: null,
      amount: 0,
    });

    if (mode === 'code' && this.deliveryCodes.length === 0) {
      this.loadAllUnpaidDeliveryCodes();
    }
  }

  switchToAdvanceWithCurrentCustomer() {
    const currentCustomer = this.billCollectionForm.get('customerId')?.value;
    this.searchMode = 'advance';
    this.unpaidDeliveries = [];
    this.selectedDeliveries.clear();
    if (currentCustomer) {
      this.billCollectionForm.patchValue({
        customerId: currentCustomer,
        amount: 0,
      });
      this.loadCustomerBookings(currentCustomer);
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
    const total = this.selectedTotal;
    if (this.searchMode !== 'advance') {
      this.billCollectionForm.patchValue({ amount: total });
    }
  }

  toggleSelectAll() {
    if (this.selectedCount === this.unpaidDeliveries.length) {
      this.selectedDeliveries.clear();
    } else {
      this.unpaidDeliveries.forEach((d) => this.selectedDeliveries.add(d.id));
    }
    this.updateTotalAmount();
  }

  getDeliveryDue(d: IDeliveryResponse): number {
    if (d.dueAmount !== undefined && d.dueAmount !== null) {
      return d.dueAmount;
    }
    const total =
      (d.chargeAmount || 0) + (d.labourCharge || 0) + (d.adjustmentValue || 0);
    const paid = d.paidAmount || 0;
    return Math.max(0, total - paid);
  }

  getDeliveryTotal(d: IDeliveryResponse): number {
    return (
      (d.chargeAmount || 0) + (d.labourCharge || 0) + (d.adjustmentValue || 0)
    );
  }

  get selectedTotal(): number {
    return this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => sum + this.getDeliveryDue(d), 0);
  }

  get selectedGrossTotal(): number {
    return this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => sum + this.getDeliveryTotal(d), 0);
  }

  get selectedPaid(): number {
    return this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => sum + (d.paidAmount || 0), 0);
  }

  get selectedCharges(): number {
    return this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => {
        const labour = d.labourCharge || 0;
        const paid = d.paidAmount || 0;
        const totalRent = (d.chargeAmount || 0) + (d.adjustmentValue || 0);
        const rentPaid = Math.max(0, paid - labour);
        return sum + Math.max(0, totalRent - rentPaid);
      }, 0);
  }

  get selectedLabour(): number {
    return this.unpaidDeliveries
      .filter((d) => this.selectedDeliveries.has(d.id))
      .reduce((sum, d) => {
        const labour = d.labourCharge || 0;
        const paid = d.paidAmount || 0;
        return sum + Math.max(0, labour - paid);
      }, 0);
  }

  get selectedCount(): number {
    return this.selectedDeliveries.size;
  }

  get formAmount(): number {
    return Number(this.billCollectionForm?.get('amount')?.value) || 0;
  }

  get advanceExcess(): number {
    if (this.searchMode === 'advance') {
      return this.formAmount;
    }
    return Math.max(0, this.formAmount - this.selectedTotal);
  }

  get isSubmitDisabled(): boolean {
    if (this.isSubmitting) return true;
    if (this.formAmount <= 0) return true;
    if (this.searchMode === 'advance') {
      return !this.billCollectionForm.get('customerId')?.value;
    }
    if (this.selectedDeliveries.size === 0) return true;
    if (this.formAmount < this.selectedTotal - 0.01) return true;
    return false;
  }

  loadExistingTransaction(id: string) {
    this.isLoading = true;
    this.transactionService.getById(id).subscribe({
      next: (transaction) => {
        this.billCollectionForm.patchValue({
          id: transaction.id,
          transactionCode: transaction.transactionCode,
          transactionDate: new Date(transaction.transactionDate)
            .toISOString()
            .split('T')[0],
          branchId: transaction.branchId,
          customerId: transaction.customerId,
          bookingId: transaction.bookingId,
          amount: transaction.amount,
          paymentMethod: transaction.paymentMethod,
          bankId: (transaction as any).bankId || null,
          paymentReference: transaction.paymentReference,
          note: transaction.note,
        });

        if (transaction.customerId) {
          this.loadCustomerBookings(transaction.customerId);
        }

        // Load deliveries associated with this transaction
        this.deliveryService.getDeliveriesByTransactionId(id).subscribe({
          next: (deliveries) => {
            this.unpaidDeliveries = deliveries;
            deliveries.forEach((d) => this.selectedDeliveries.add(d.id));
            if (deliveries.length === 0) {
              this.searchMode = 'advance';
            }
            this.isLoading = false;
          },
          error: (err) => {
            console.error('Failed to load deliveries:', err);
            this.isLoading = false;
          },
        });
      },
      error: (err) => {
        console.error('Failed to load transaction:', err);
        this.toastr.error('Failed to load transaction');
        this.isLoading = false;
        this.router.navigate(['/bill-collection/list']);
      },
    });
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

  onSubmit(printAfterSave: boolean = false) {
    if (this.billCollectionForm.invalid) {
      this.billCollectionForm.markAllAsTouched();
      this.toastr.error('Please fill all required fields');
      return;
    }

    const formValue = this.billCollectionForm.getRawValue();

    if (this.searchMode === 'advance') {
      if (!formValue.customerId) {
        this.toastr.error('Please select a customer for advance payment');
        return;
      }
      if (formValue.amount <= 0) {
        this.toastr.error('Advance amount must be greater than 0');
        return;
      }
    } else {
      if (this.selectedDeliveries.size === 0) {
        this.toastr.error(
          'Please select at least one delivery or switch to Advance mode',
        );
        return;
      }
      if (formValue.amount < this.selectedTotal - 0.01) {
        this.toastr.error(
          `Amount (৳${formValue.amount}) cannot be less than total delivery due (৳${this.selectedTotal})`,
        );
        return;
      }
    }

    this.isSubmitting = true;
    this.saveAndPrint = printAfterSave;

    const advanceAmount =
      this.searchMode === 'advance'
        ? Number(formValue.amount)
        : Math.max(0, Number(formValue.amount) - this.selectedTotal);

    const payload: IDeliveryBillCollectionRequest = {
      transactionCode: formValue.transactionCode,
      transactionDate: formValue.transactionDate,
      branchId: formValue.branchId,
      deliveryIds:
        this.searchMode === 'advance' ? [] : Array.from(this.selectedDeliveries),
      amount: Number(formValue.amount),
      paymentMethod: formValue.paymentMethod,
      bankId: formValue.bankId,
      paymentReference: formValue.paymentReference,
      note: formValue.note,
      customerId: formValue.customerId,
      bookingId: formValue.bookingId,
      advanceAmount: advanceAmount,
    };

    this.billCollectionService.createDeliveryBillCollection(payload).subscribe({
      next: (response) => {
        // this.toastr.success(
        //   advanceAmount > 0
        //     ? 'Bill collection & advance recorded successfully!'
        //     : 'Delivery bill collection created successfully!',
        // );
        if (printAfterSave) {
          this.router.navigate([
            '/transaction/receipt-print',
            response.id,
            'bill-collection-list',
          ]);
        } else {
          this.router.navigate(['/bill-collection/list']);
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Failed to create bill collection:', err);
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
    this.customerBookings = [];
    this.initForm();
    this.generateTransactionCode();
  }
}
