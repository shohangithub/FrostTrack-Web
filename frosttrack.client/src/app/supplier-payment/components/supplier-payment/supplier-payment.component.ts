import { Component, OnInit, ViewChild } from '@angular/core';
import {
  DatatableComponent,
  NgxDatatableModule,
} from '@swimlane/ngx-datatable';
import {
  FormsModule,
  ReactiveFormsModule,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators,
} from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrModule } from 'ngx-toastr';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SupplierPaymentService } from 'app/supplier-payment/services/supplier-payment.service';
import {
  ISupplierPaymentRequest,
  ISupplierPaymentResponse,
} from 'app/supplier-payment/models/supplier-payment.interface';
import { BranchService } from 'app/administration/services/branch.service';
import { ILookup } from '@core/models/lookup';
import { NgSelectModule } from '@ng-select/ng-select';
import { SupplierService } from 'app/common/services/supplier.service';
import { ISupplierListResponse } from 'app/common/models/supplier.interface';
import { PaymentMethodService } from 'app/common/services/payment-method.service';
import { BankService } from 'app/common/services/bank.service';
import { AuthService } from '@core/service/auth.service';
import { LayoutService } from '@core/service/layout.service';
import { PrintService, IPrintOptions } from 'app/common/services/print.service';
import { IPaymentReceiptData } from 'app/common/components/payment-receipt/payment-receipt.component';
import { PrintHelperService, IPrintResult } from 'app/shared/modules/print';
import { BaseComponent } from '@core/base/base-component';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-supplier-payment',
  templateUrl: './supplier-payment.component.html',
  styles: [
    `
      .payment-method-card {
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }

      .payment-method-card:hover {
        box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
        transform: translateY(-2px);
      }

      .payment-method-card.border-primary {
        box-shadow: 0 4px 12px rgba(13, 110, 253, 0.3);
      }
    `,
  ],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NgSelectModule,
    NgxDatatableModule,
    ToastrModule,
  ],
})
export class SupplierPaymentComponent extends BaseComponent implements OnInit {
  @ViewChild(DatatableComponent, { static: false }) table!: DatatableComponent;

  register!: UntypedFormGroup;
  rows: any[] = [];
  loadingIndicator = true;
  reorderable = true;
  paymentId: number = 0;
  isEditMode: boolean = false;

  // Data sources
  suppliers: ISupplierListResponse[] = [];
  banks: ILookup<number>[] = [];
  branchs: ILookup<number>[] = [];

  // Selected entities
  selectedSupplier: ISupplierListResponse | null = null;
  supplierDueBalance: number = 0;

  // Summary calculations
  paymentSummary = {
    totalDueAmount: 0,
    currentPaymentAmount: 0,
    remainingDueAmount: 0,
  };

  // Loading states
  supplierLoading = false;
  bankLoading = false;
  dueBalanceLoading = false;

  // Payment methods
  paymentMethods: {
    value: string;
    text: string;
    icon?: string;
    colorClass?: string;
    description?: string;
    requiresBankAccount?: boolean;
    requiresCheckDetails?: boolean;
    requiresOnlineDetails?: boolean;
    requiresMobileWalletDetails?: boolean;
    requiresCardDetails?: boolean;
  }[] = [
    {
      value: 'Cash',
      text: 'Cash',
      icon: 'fa fa-money-bill-wave',
      colorClass: 'text-success',
      description: 'Payment Method',
    },
  ];

  // Online payment methods
  onlinePaymentMethods = [
    { value: 'PayPal', text: 'PayPal' },
    { value: 'Stripe', text: 'Stripe' },
    { value: 'Razorpay', text: 'Razorpay' },
    { value: 'Bank_Gateway', text: 'Bank Gateway' },
  ];

  // Mobile wallet options
  mobileWallets = [
    { value: 'bKash', text: 'bKash' },
    { value: 'Nagad', text: 'Nagad' },
    { value: 'Rocket', text: 'Rocket' },
    { value: 'SureCash', text: 'SureCash' },
    { value: 'PayPal', text: 'PayPal' },
    { value: 'GooglePay', text: 'Google Pay' },
    { value: 'ApplePay', text: 'Apple Pay' },
  ];

  // Card types
  cardTypes = [
    { value: 'Visa', text: 'Visa' },
    { value: 'MasterCard', text: 'MasterCard' },
    { value: 'AmericanExpress', text: 'American Express' },
    { value: 'Discover', text: 'Discover' },
    { value: 'DinersClub', text: 'Diners Club' },
  ];

  constructor(
    private fb: UntypedFormBuilder,
    private supplierPaymentService: SupplierPaymentService,
    private supplierService: SupplierService,
    private bankService: BankService,
    private branchService: BranchService,
    private paymentMethodService: PaymentMethodService,
    private route: ActivatedRoute,
    private modalService: NgbModal,
    private authService: AuthService,
    private layoutService: LayoutService,
    private printService: PrintService,
    private printHelper: PrintHelperService,
    private toastr: ToastrService
  ) {
    super();
    this.register = this.createForm();
  }

  ngOnInit(): void {
    this.checkEditMode();
    this.loadInitialData();
  }

  createForm(): UntypedFormGroup {
    return this.fb.group({
      id: [0],
      paymentNumber: ['', Validators.required],
      paymentDate: [
        new Date().toISOString().split('T')[0],
        Validators.required,
      ],
      paymentType: ['Supplier', Validators.required],
      supplierId: [null, Validators.required],
      paymentMethod: ['Cash', Validators.required],
      bankId: [null],
      checkNumber: [''],
      checkDate: [''],
      // Online payment fields
      onlinePaymentMethod: [''],
      transactionId: [''],
      gatewayReference: [''],
      // Mobile wallet fields
      mobileWalletType: [''],
      walletNumber: [''],
      walletTransactionId: [''],
      // Card payment fields
      cardType: [''],
      cardLastFour: [''],
      cardTransactionId: [''],
      paymentAmount: [0, [Validators.required, Validators.min(0.01)]],
      notes: [''],
      branchId: [1], // Default branch ID
    });
  }

  loadInitialData(): void {
    // Load data reactively using subscriptions
    this.generatePaymentNumber();
    this.loadBranches();
    this.loadSuppliers();
    this.loadBanks();
    this.loadPaymentMethods();
  }

  checkEditMode(): void {
    this.route.params
      .pipe(this.takeUntilDestroyed())
      .subscribe((params: any) => {
        if (params['id']) {
          this.paymentId = +params['id'];
          this.isEditMode = true;
          this.loadPaymentData(this.paymentId);
        }
      });
  }

  generatePaymentNumber(): void {
    this.supplierPaymentService
      .generatePaymentNumber()
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.register.patchValue({ paymentNumber: response.code });
          }
        },
        error: (error) => {
          console.error('Error generating payment number:', error);
        },
      });
  }

  loadBranches(): void {
    this.branchService
      .getLookup()
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.branchs = response;
          }
        },
        error: (error) => {
          console.error('Error loading branches:', error);
        },
      });
  }

  loadSuppliers(): void {
    this.supplierLoading = true;
    this.supplierService
      .getList()
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.suppliers = response;
          }
          this.supplierLoading = false;
        },
        error: (error) => {
          console.error('Error loading suppliers:', error);
          this.supplierLoading = false;
        },
      });
  }

  loadBanks(): void {
    this.bankLoading = true;
    this.bankService
      .getLookup()
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.banks = response;
          }
          this.bankLoading = false;
        },
        error: (error) => {
          console.error('Error loading banks:', error);
          this.bankLoading = false;
        },
      });
  }

  loadPaymentMethods(): void {
    this.paymentMethodService
      .getActiveList()
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.paymentMethods = response.map((pm: any) => {
              // Use iconClass from API if available, otherwise fallback to category-based mapping
              let icon = pm.iconClass || 'fa fa-money-bill-wave';
              let colorClass = 'text-success';
              const description = pm.description || 'Payment method';

              // Determine color class based on category
              switch (pm.category?.toLowerCase()) {
                case 'cash':
                  colorClass = 'text-success';
                  if (!pm.iconClass) icon = 'fa fa-money-bill-wave';
                  break;
                case 'bank':
                  colorClass = 'text-primary';
                  if (!pm.iconClass) icon = 'fa fa-university';
                  break;
                case 'card':
                  colorClass = 'text-warning';
                  if (!pm.iconClass) icon = 'fa fa-credit-card';
                  break;
                case 'mobilewallet':
                  colorClass = 'text-info';
                  if (!pm.iconClass) icon = 'fa fa-mobile-alt';
                  break;
                default:
                  colorClass = 'text-secondary';
                  if (!pm.iconClass) icon = 'fa fa-money-bill-wave';
                  break;
              }

              return {
                value: pm.methodName,
                text: pm.methodName,
                icon: icon,
                colorClass: colorClass,
                description: description,
                requiresBankAccount: pm.requiresBankAccount,
                requiresCheckDetails: pm.requiresCheckDetails,
                requiresOnlineDetails: pm.requiresOnlineDetails,
                requiresMobileWalletDetails: pm.requiresMobileWalletDetails,
                requiresCardDetails: pm.requiresCardDetails,
              };
            });
          }
        },
        error: (error) => {
          console.error('Error loading payment methods:', error);
          // Keep default payment methods if API fails
        },
      });
  }

  loadPaymentData(id: number): void {
    this.supplierPaymentService
      .getById(id)
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.populateForm(response);
          }
        },
        error: (error) => {
          console.error('Error loading payment data:', error);
        },
      });
  }

  populateForm(payment: ISupplierPaymentResponse): void {
    this.register.patchValue({
      id: payment.id,
      paymentNumber: payment.paymentNumber,
      paymentDate: payment.paymentDate,
      paymentType: payment.paymentType,
      supplierId: payment.supplierId,
      paymentMethod: payment.paymentMethod,
      bankId: payment.bankId,
      checkNumber: payment.checkNumber,
      checkDate: payment.checkDate,
      paymentAmount: payment.paymentAmount,
      notes: payment.notes,
      branchId: payment.branchId,
    });
  }

  onPaymentTypeChange(): void {
    // Always supplier payment in this component
  }

  onSupplierChange(): void {
    const supplierId = this.register.get('supplierId')?.value;
    if (supplierId) {
      this.selectedSupplier =
        this.suppliers.find((s) => s.id === supplierId) || null;
      this.loadSupplierDueBalance(supplierId);
    } else {
      this.selectedSupplier = null;
      this.supplierDueBalance = 0;
      this.resetPaymentSummary();
    }
  }

  loadSupplierDueBalance(supplierId: number): void {
    this.dueBalanceLoading = true;
    this.supplierPaymentService
      .getSupplierDueBalance(supplierId)
      .pipe(this.takeUntilDestroyed())
      .subscribe({
        next: (response: any) => {
          if (response) {
            this.supplierDueBalance = response;
            this.calculatePaymentSummary();
          }
          this.dueBalanceLoading = false;
        },
        error: (error) => {
          console.error('Error loading supplier due balance:', error);
          this.supplierDueBalance = 0;
          this.dueBalanceLoading = false;
        },
      });
  }

  async savePayment(): Promise<void> {
    if (this.register.valid) {
      try {
        const formValue = this.register.value;

        // Create simplified payment request without payment details
        const paymentRequest: ISupplierPaymentRequest = {
          ...formValue,
        };

        if (this.isEditMode) {
          const response = await firstValueFrom(
            this.supplierPaymentService.update(this.paymentId, paymentRequest)
          );
          if (response) {
            this.paymentId = response.id || this.paymentId; // Update paymentId if returned from API
          }
        } else {
          const response = await firstValueFrom(
            this.supplierPaymentService.create(paymentRequest)
          );
          if (response) {
            this.paymentId = response.id || 0; // Set paymentId from response
            this.register.reset();
            this.register = this.createForm();
            this.generatePaymentNumber();
            this.selectedSupplier = null;
            this.supplierDueBalance = 0;
            this.resetPaymentSummary();
          }
        }
      } catch (error: any) {
        // Service handles error messages automatically
        console.error('Payment creation error:', error);
      }
    } else {
      this.markFormGroupTouched(this.register);
      console.warn('Form validation failed - required fields missing');
    }
  }

  private markFormGroupTouched(formGroup: UntypedFormGroup): void {
    Object.keys(formGroup.controls).forEach((field) => {
      const control = formGroup.get(field);
      control?.markAsTouched({ onlySelf: true });
    });
  }

  resetForm(): void {
    this.register.reset();
    this.register = this.createForm();
    this.generatePaymentNumber();
  }

  calculatePaymentSummary(): void {
    if (this.selectedSupplier) {
      this.paymentSummary.totalDueAmount = this.supplierDueBalance;
      this.paymentSummary.currentPaymentAmount =
        this.register.get('paymentAmount')?.value || 0;
      this.paymentSummary.remainingDueAmount =
        this.paymentSummary.totalDueAmount -
        this.paymentSummary.currentPaymentAmount;
    }
  }

  resetPaymentSummary(): void {
    this.paymentSummary = {
      totalDueAmount: 0,
      currentPaymentAmount: 0,
      remainingDueAmount: 0,
    };
  }

  // Helper methods for dynamic payment method details
  getSelectedPaymentMethod() {
    const selectedValue = this.register.get('paymentMethod')?.value;
    return this.paymentMethods.find((pm) => pm.value === selectedValue);
  }

  requiresBankAccount(): boolean {
    const method = this.getSelectedPaymentMethod();
    return method?.requiresBankAccount || false;
  }

  requiresCheckDetails(): boolean {
    const method = this.getSelectedPaymentMethod();
    return method?.requiresCheckDetails || false;
  }

  requiresOnlineDetails(): boolean {
    const method = this.getSelectedPaymentMethod();
    return method?.requiresOnlineDetails || false;
  }

  requiresMobileWalletDetails(): boolean {
    const method = this.getSelectedPaymentMethod();
    return method?.requiresMobileWalletDetails || false;
  }

  requiresCardDetails(): boolean {
    const method = this.getSelectedPaymentMethod();
    return method?.requiresCardDetails || false;
  }

  // Save and Print functionality
  async saveAndPrint(): Promise<void> {
    if (this.register.valid && this.selectedSupplier) {
      try {
        await this.savePayment();
        // Print receipt with current form data (paymentId might be set from save response)
        await this.printCurrentPaymentReceipt();
      } catch (error) {
        console.error('Error in save and print:', error);
        this.toastr?.error('Error occurred while saving and printing payment');
      }
    } else {
      this.toastr?.error(
        'Please complete the form and select a supplier before saving and printing'
      );
    }
  }

  // Print receipt for current payment
  async printCurrentPaymentReceipt(): Promise<void> {
    try {
      // Get current branch ID
      const branchId = this.register.get('branchId')?.value || 1;

      // Convert form data to payment data format
      const paymentData = this.convertFormToPaymentData();

      // Use the new modular print system
      const result: IPrintResult = await this.printHelper.printSupplierPayment(
        paymentData,
        branchId
      );

      if (result.success) {
        this.toastr?.success('Payment receipt printed successfully');
      } else {
        this.toastr?.error(result.message || 'Failed to print receipt');
        console.error('Print error:', result.error);
      }
    } catch (error) {
      console.error('Error printing payment receipt:', error);
      this.toastr?.error('Error occurred while printing payment receipt');
    }
  }

  // Convert form data to payment data format for new print system
  private convertFormToPaymentData(): any {
    const formValue = this.register.value;
    const selectedBank = this.banks.find(
      (b: any) => b.value === formValue.bankId
    );
    const selectedBranch = this.branchs.find(
      (b: any) => b.value === formValue.branchId
    );

    return {
      // Payment Information
      id: this.paymentId || Date.now(),
      paymentNumber: formValue.paymentNumber || this.generateReceiptNumber(),
      receiptNumber: formValue.paymentNumber || this.generateReceiptNumber(),
      paymentDate: formValue.paymentDate || new Date(),
      paymentAmount: formValue.paymentAmount || 0,
      paymentMethod: formValue.paymentMethod,
      referenceNumber: formValue.referenceNumber || formValue.paymentNumber,
      notes: formValue.notes,

      // Supplier Information
      supplierId: formValue.supplierId,
      supplierName: this.selectedSupplier?.supplierName || 'Unknown Supplier',
      supplierAddress: this.selectedSupplier?.address,
      supplierPhone:
        this.selectedSupplier?.supplierMobile ||
        this.selectedSupplier?.officePhone,
      supplierEmail: this.selectedSupplier?.supplierEmail,

      // Branch Information
      branchId: formValue.branchId,
      branchName: selectedBranch?.text || this.getCurrentBranchName(),
      branchAddress: 'Branch Address', // You might want to get this from branch data
      branchPhone: 'Branch Phone', // You might want to get this from branch data

      // Bank Information (if applicable)
      bankId: formValue.bankId,
      bankName: selectedBank?.text,
      accountNumber: formValue.accountNumber,

      // Additional payment details
      checkNumber: formValue.checkNumber,
      checkDate: formValue.checkDate,
      onlineTransactionId: formValue.transactionId,
      mobileWalletNumber: formValue.walletNumber,
      cardLastFourDigits: formValue.cardLastFour,

      // Payment breakdown
      totalAmount: formValue.paymentAmount || 0,
      paidAmount: formValue.paymentAmount || 0,
      previousDue: this.paymentSummary?.totalDueAmount || 0,
      remainingDue: this.paymentSummary?.remainingDueAmount || 0,

      // System Information
      createdBy: 'System User', // Simplified user info
      createdAt: new Date(),
      printDateTime: new Date(),
    };
  }

  private generateReceiptNumber(): string {
    const prefix = 'SP'; // Supplier Payment
    const timestamp = Date.now().toString().slice(-6);
    const random = Math.random().toString(36).substring(2, 5).toUpperCase();
    return `${prefix}-${timestamp}-${random}`;
  }

  // Convert payment response to receipt data format
  private convertToReceiptData(
    payment: ISupplierPaymentResponse
  ): IPaymentReceiptData {
    const formValue = this.register.value;
    const selectedBank = this.banks.find((b) => b.value === formValue.bankId);

    return {
      id: payment.id,
      paymentNumber: payment.paymentNumber,
      paymentDate:
        typeof payment.paymentDate === 'string'
          ? payment.paymentDate
          : payment.paymentDate.toISOString().split('T')[0],
      paymentType: payment.paymentType,
      supplier: {
        name: this.selectedSupplier?.supplierName || 'Unknown Supplier',
        phone:
          this.selectedSupplier?.supplierMobile ||
          this.selectedSupplier?.officePhone,
        address: this.selectedSupplier?.address,
        email: this.selectedSupplier?.supplierEmail,
      },
      paymentMethod: formValue.paymentMethod,
      paymentDetails: {
        bankName: selectedBank?.text,
        checkNumber: formValue.checkNumber,
        checkDate: formValue.checkDate,
        onlinePaymentMethod: formValue.onlinePaymentMethod,
        transactionId: formValue.transactionId,
        gatewayReference: formValue.gatewayReference,
        mobileWalletType: formValue.mobileWalletType,
        walletNumber: formValue.walletNumber,
        walletTransactionId: formValue.walletTransactionId,
        cardType: formValue.cardType,
        cardLastFour: formValue.cardLastFour,
        cardTransactionId: formValue.cardTransactionId,
      },
      paymentAmount: payment.paymentAmount,
      previousDue: this.paymentSummary.totalDueAmount,
      remainingDue: this.paymentSummary.remainingDueAmount,
      notes: payment.notes,
      createdBy: 'Current User',
      branchName: this.getCurrentBranchName(),
      printDateTime: new Date().toISOString(),
    };
  }

  // Convert current form data to receipt format
  private convertFormToReceiptData(): IPaymentReceiptData {
    const formValue = this.register.value;
    const selectedBank = this.banks.find((b) => b.value === formValue.bankId);

    return {
      id: this.paymentId || 0,
      paymentNumber: formValue.paymentNumber || '',
      paymentDate:
        formValue.paymentDate || new Date().toISOString().split('T')[0],
      paymentType: formValue.paymentType || 'Supplier',
      supplier: {
        name: this.selectedSupplier?.supplierName || 'Unknown Supplier',
        phone:
          this.selectedSupplier?.supplierMobile ||
          this.selectedSupplier?.officePhone,
        address: this.selectedSupplier?.address,
        email: this.selectedSupplier?.supplierEmail,
      },
      paymentMethod: formValue.paymentMethod,
      paymentDetails: {
        bankName: selectedBank?.text,
        checkNumber: formValue.checkNumber,
        checkDate: formValue.checkDate,
        onlinePaymentMethod: formValue.onlinePaymentMethod,
        transactionId: formValue.transactionId,
        gatewayReference: formValue.gatewayReference,
        mobileWalletType: formValue.mobileWalletType,
        walletNumber: formValue.walletNumber,
        walletTransactionId: formValue.walletTransactionId,
        cardType: formValue.cardType,
        cardLastFour: formValue.cardLastFour,
        cardTransactionId: formValue.cardTransactionId,
      },
      paymentAmount: formValue.paymentAmount || 0,
      previousDue: this.paymentSummary.totalDueAmount,
      remainingDue: this.paymentSummary.remainingDueAmount,
      notes: formValue.notes,
      createdBy: 'Current User',
      branchName: this.getCurrentBranchName(),
      printDateTime: new Date().toISOString(),
    };
  }

  // Get current payment data for printing
  private async getCurrentPaymentData(): Promise<ISupplierPaymentResponse | null> {
    if (this.paymentId && this.isEditMode) {
      try {
        // In a real app, you'd fetch the payment by ID
        // For now, create from form data
        const formValue = this.register.value;
        return {
          id: this.paymentId,
          paymentNumber: formValue.paymentNumber,
          paymentDate: formValue.paymentDate,
          paymentType: formValue.paymentType,
          supplierId: formValue.supplierId,
          paymentMethod: formValue.paymentMethod,
          paymentAmount: formValue.paymentAmount,
          notes: formValue.notes,
          branchId: formValue.branchId,
        } as ISupplierPaymentResponse;
      } catch (error) {
        console.error('Error fetching payment data:', error);
        return null;
      }
    }
    return null;
  }

  // Generate receipt HTML
  private generateReceiptHTML(
    receiptData: IPaymentReceiptData,
    options: Partial<IPrintOptions>
  ): string {
    // This is a simplified HTML generation. In a real app, you might use Angular's
    // ComponentFactory or a template engine to generate this dynamically
    return `
      <div class="print-container">
        <!-- Header -->
        <div class="print-header">
          <h1>${options.header?.companyName || 'Company Name'}</h1>
          <div class="company-info">
            <div>${options.header?.companyAddress || ''}</div>
            <div>Phone: ${options.header?.companyPhone || ''}</div>
            <div>Email: ${options.header?.companyEmail || ''}</div>
          </div>
          ${
            options.header?.branchName
              ? `
            <hr style="margin: 10px 0; border: 1px solid #ccc;">
            <div><strong>Branch: ${options.header.branchName}</strong></div>
            ${
              options.header.branchAddress
                ? `<div>${options.header.branchAddress}</div>`
                : ''
            }
          `
              : ''
          }
          <h2 style="margin: 15px 0 10px 0; font-size: 18px;">PAYMENT RECEIPT</h2>
        </div>

        <!-- Content -->
        <div class="print-content">
          <!-- Receipt Info -->
          <div class="receipt-info mb-3">
            <div class="row">
              <span class="col-label">Receipt No:</span>
              <span class="col-value">${receiptData.paymentNumber}</span>
            </div>
            <div class="row">
              <span class="col-label">Payment Date:</span>
              <span class="col-value">${new Date(
                receiptData.paymentDate
              ).toLocaleDateString()}</span>
            </div>
            <div class="row">
              <span class="col-label">Print Date:</span>
              <span class="col-value">${new Date().toLocaleString()}</span>
            </div>
          </div>

          <!-- Supplier Info -->
          <div class="supplier-info mb-3">
            <h3 style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;">Supplier Information</h3>
            <div class="row">
              <span class="col-label">Supplier Name:</span>
              <span class="col-value">${receiptData.supplier.name}</span>
            </div>
            ${
              receiptData.supplier.phone
                ? `
              <div class="row">
                <span class="col-label">Phone:</span>
                <span class="col-value">${receiptData.supplier.phone}</span>
              </div>
            `
                : ''
            }
          </div>

          <!-- Payment Details -->
          <div class="payment-details mb-3">
            <h3 style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;">Payment Details</h3>
            <table class="receipt-table">
              <tr>
                <td><strong>Payment Method:</strong></td>
                <td>${receiptData.paymentMethod}</td>
              </tr>
              ${
                receiptData.paymentDetails?.bankName
                  ? `
                <tr>
                  <td><strong>Bank Name:</strong></td>
                  <td>${receiptData.paymentDetails.bankName}</td>
                </tr>
              `
                  : ''
              }
              ${
                receiptData.paymentDetails?.checkNumber
                  ? `
                <tr>
                  <td><strong>Check Number:</strong></td>
                  <td>${receiptData.paymentDetails.checkNumber}</td>
                </tr>
              `
                  : ''
              }
            </table>
          </div>

          <!-- Amount Summary -->
          <div class="amount-summary mb-3">
            <h3 style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;">Amount Summary</h3>
            <table class="receipt-table">
              <tr>
                <td><strong>Previous Due:</strong></td>
                <td class="text-right">$${receiptData.previousDue.toFixed(
                  2
                )}</td>
              </tr>
              <tr class="amount-row">
                <td><strong>Payment Amount:</strong></td>
                <td class="text-right"><strong>$${receiptData.paymentAmount.toFixed(
                  2
                )}</strong></td>
              </tr>
              <tr>
                <td><strong>Remaining Due:</strong></td>
                <td class="text-right">$${receiptData.remainingDue.toFixed(
                  2
                )}</td>
              </tr>
            </table>
          </div>

          ${
            receiptData.notes
              ? `
            <div class="notes mb-3">
              <h3 style="margin: 0 0 10px 0; font-size: 16px; border-bottom: 1px solid #333;">Notes</h3>
              <p style="margin: 0; padding: 10px; background: #f8f9fa;">${receiptData.notes}</p>
            </div>
          `
              : ''
          }
        </div>

        <!-- Footer -->
        <div class="print-footer">
          <div><strong>${
            options.footer?.thankYouMessage || 'Thank you for your payment!'
          }</strong></div>
          <div>${
            options.footer?.footerText || 'Thank you for your business!'
          }</div>
          <div style="margin-top: 20px;">
            <div>Processed by: ${receiptData.createdBy}</div>
            <div>Branch: ${receiptData.branchName}</div>
          </div>
        </div>
      </div>
    `;
  }

  // Get current branch name
  private getCurrentBranchName(): string {
    const branchId = this.register.get('branchId')?.value;
    const branch = this.branchs.find((b) => b.value === branchId);
    return branch?.text || 'Main Branch';
  }

  // Show print preview
  showPrintPreview(): void {
    if (!this.selectedSupplier) {
      this.toastr?.warning('Please select a supplier first');
      return;
    }

    // Create preview data from current form
    const formValue = this.register.value;
    const receiptData: IPaymentReceiptData = {
      id: 0,
      paymentNumber: formValue.paymentNumber || 'PREVIEW',
      paymentDate: formValue.paymentDate || new Date().toISOString(),
      paymentType: formValue.paymentType || 'Supplier',
      supplier: {
        name: this.selectedSupplier.supplierName,
        phone:
          this.selectedSupplier.supplierMobile ||
          this.selectedSupplier.officePhone,
        address: this.selectedSupplier.address,
        email: this.selectedSupplier.supplierEmail,
      },
      paymentMethod: formValue.paymentMethod,
      paymentDetails: {
        bankName: this.banks.find((b) => b.value === formValue.bankId)?.text,
        checkNumber: formValue.checkNumber,
        checkDate: formValue.checkDate,
      },
      paymentAmount: formValue.paymentAmount || 0,
      previousDue: this.paymentSummary.totalDueAmount,
      remainingDue: this.paymentSummary.remainingDueAmount,
      notes: formValue.notes,
      createdBy: 'Preview User',
      branchName: this.getCurrentBranchName(),
      printDateTime: new Date().toISOString(),
    };

    const previewHTML = this.generateReceiptHTML(receiptData, {
      header: {
        companyName: 'Your Company Name (Preview)',
        companyAddress: '123 Business St, City, State',
        companyPhone: '+1 (555) 123-4567',
        companyEmail: 'info@yourcompany.com',
        branchName: this.getCurrentBranchName(),
      },
      footer: {
        footerText: 'Thank you for your business!',
        thankYouMessage: 'Payment received successfully!',
      },
    });

    this.printService.showPrintPreview(previewHTML);
  }
}
