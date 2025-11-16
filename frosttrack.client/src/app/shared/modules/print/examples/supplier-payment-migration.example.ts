/**
 * Example of how to update the existing supplier payment component to use the new modular print system
 *
 * This file shows the changes needed to replace the old print service with the new modular print functionality
 */

// OLD IMPORTS (Replace these)
// import { PrintService, IPrintOptions } from 'app/common/services/print.service';
// import { IPaymentReceiptData } from 'app/common/components/payment-receipt/payment-receipt.component';

// NEW IMPORTS (Use these instead)
import {
  PrintHelperService,
  PrintService,
  PrintPreviewComponent,
  IPrintPreviewData,
  IPrintResult,
  PrintReportType,
  PrintFormat,
} from 'app/shared/modules/print';

// In the component class:
export class SupplierPaymentComponent implements OnInit {
  // Add preview modal properties
  showPrintPreview = false;
  printPreviewData: IPrintPreviewData | null = null;

  constructor(
    // ... existing dependencies
    private printHelper: PrintHelperService, // Add this instead of old PrintService
    private printService: PrintService // Keep this for direct access if needed
  ) {}

  // OLD METHOD (Replace this)
  /*
  async printCurrentPaymentReceipt(): Promise<void> {
    try {
      // Get print settings for current branch
      const branchId = this.paymentForm.get('branchId')?.value || 1;
      let printOptions: Partial<IPrintOptions> = {};
      
      try {
        printOptions = (await this.printService.getPrintSettings(branchId).toPromise()) || {};
      } catch (error) {
        console.warn('Could not load print settings, using defaults:', error);
        // Use default print settings
      }

      // Generate receipt data
      const receiptData: IPaymentReceiptData = this.generateReceiptData();
      
      // Print the receipt
      await this.printService.printPaymentReceipt(receiptData, printOptions);
      this.toastr.success('Payment receipt printed successfully');
    } catch (error) {
      console.error('Error printing payment receipt:', error);
      this.toastr.error('Error occurred while printing payment receipt');
    }
  }
  */

  // NEW METHOD (Use this instead)
  async printCurrentPaymentReceipt(): Promise<void> {
    try {
      const branchId = this.paymentForm.get('branchId')?.value || 1;
      const paymentData = this.generateReceiptData();

      // Use the helper service for quick printing
      const result = await this.printHelper.printSupplierPayment(
        paymentData,
        branchId
      );

      if (result.success) {
        this.toastr.success('Payment receipt printed successfully');
      } else {
        this.toastr.error(result.message || 'Failed to print receipt');
      }
    } catch (error) {
      console.error('Error printing payment receipt:', error);
      this.toastr.error('Error occurred while printing payment receipt');
    }
  }

  // NEW METHOD - Show print preview
  async showPrintReceiptPreview(): Promise<void> {
    try {
      const branchId = this.paymentForm.get('branchId')?.value || 1;
      const paymentData = this.generateReceiptData();

      // Generate preview data
      this.printPreviewData = await this.printHelper.showPreview(
        paymentData,
        PrintReportType.SupplierPayment,
        branchId
      );

      this.showPrintPreview = true;
    } catch (error) {
      console.error('Error generating print preview:', error);
      this.toastr.error('Error occurred while generating print preview');
    }
  }

  // NEW METHOD - Handle print from preview
  onPrintFromPreview(event: { format: PrintFormat; settings: any }): void {
    // This will be called when user clicks print in the preview modal
    console.log('Printing with format:', event.format);
    console.log('Using settings:', event.settings);
  }

  // NEW METHOD - Handle print completion
  onPrintCompleted(result: IPrintResult): void {
    if (result.success) {
      this.toastr.success('Payment receipt printed successfully');
      this.showPrintPreview = false;
    } else {
      this.toastr.error(result.message || 'Failed to print receipt');
    }
  }

  // NEW METHOD - Generate PDF
  async generatePDF(): Promise<void> {
    try {
      const branchId = this.paymentForm.get('branchId')?.value || 1;
      const paymentData = this.generateReceiptData();

      const result = await this.printHelper.generatePDF(
        paymentData,
        PrintReportType.SupplierPayment,
        branchId
      );

      if (result.success) {
        this.toastr.success('PDF generated and downloaded successfully');
      } else {
        this.toastr.error(result.message || 'Failed to generate PDF');
      }
    } catch (error) {
      console.error('Error generating PDF:', error);
      this.toastr.error('Error occurred while generating PDF');
    }
  }

  // ENHANCED METHOD - Updated receipt data generation
  private generateReceiptData(): any {
    const formValue = this.paymentForm.value;
    const selectedSupplier = this.suppliers.find(
      (s) => s.id === formValue.supplierId
    );
    const selectedBranch = this.branches.find(
      (b) => b.id === formValue.branchId
    );

    return {
      // Payment Information
      id: formValue.id || Date.now(),
      receiptNumber: this.generateReceiptNumber(),
      paymentDate: formValue.paymentDate || new Date(),
      paymentAmount: formValue.amount,
      paymentMethod: formValue.paymentMethod,
      referenceNumber: formValue.referenceNumber,
      notes: formValue.notes,

      // Supplier Information
      supplierId: formValue.supplierId,
      supplierName: selectedSupplier?.name || 'Unknown Supplier',
      supplierAddress: selectedSupplier?.address,
      supplierPhone: selectedSupplier?.phone,
      supplierEmail: selectedSupplier?.email,

      // Branch Information
      branchId: formValue.branchId,
      branchName: selectedBranch?.name,
      branchAddress: selectedBranch?.address,
      branchPhone: selectedBranch?.phone,

      // Bank Information (if applicable)
      bankId: formValue.bankId,
      bankName: this.banks.find((b) => b.id === formValue.bankId)?.name,
      accountNumber: formValue.accountNumber,

      // Additional payment details
      checkNumber: formValue.checkNumber,
      checkDate: formValue.checkDate,
      onlineTransactionId: formValue.onlineTransactionId,
      mobileWalletNumber: formValue.mobileWalletNumber,
      cardLastFourDigits: formValue.cardNumber?.slice(-4),

      // System Information
      createdBy: this.authService.getCurrentUser()?.userName,
      createdAt: new Date(),

      // Formatting helpers
      formattedAmount: this.formatCurrency(formValue.amount),
      formattedDate: this.formatDate(formValue.paymentDate),
    };
  }

  private generateReceiptNumber(): string {
    const prefix = 'PR'; // Payment Receipt
    const timestamp = Date.now().toString().slice(-6);
    const random = Math.random().toString(36).substring(2, 5).toUpperCase();
    return `${prefix}-${timestamp}-${random}`;
  }

  private formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  }

  private formatDate(date: Date | string): string {
    const d = new Date(date);
    return d.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }
}

// Template changes needed:
/*
<!-- OLD BUTTONS (Replace these) -->
<!--
<button type="button" 
        class="btn btn-success me-2" 
        (click)="saveAndPrint()" 
        [disabled]="paymentForm.invalid || isLoading">
  <i class="fa fa-print me-1"></i>
  Pay & Print
</button>
-->

<!-- NEW BUTTONS (Use these instead) -->
<div class="btn-group me-2">
  <button type="button" 
          class="btn btn-success" 
          (click)="saveAndPrint()" 
          [disabled]="paymentForm.invalid || isLoading">
    <i class="fa fa-print me-1"></i>
    Pay & Print
  </button>
  
  <button type="button" 
          class="btn btn-outline-success dropdown-toggle dropdown-toggle-split" 
          data-bs-toggle="dropdown" 
          [disabled]="paymentForm.invalid || isLoading">
    <span class="visually-hidden">Toggle Dropdown</span>
  </button>
  
  <ul class="dropdown-menu">
    <li>
      <a class="dropdown-item" href="#" (click)="showPrintReceiptPreview()">
        <i class="fa fa-eye me-1"></i>
        Print Preview
      </a>
    </li>
    <li>
      <a class="dropdown-item" href="#" (click)="generatePDF()">
        <i class="fa fa-file-pdf me-1"></i>
        Generate PDF
      </a>
    </li>
    <li><hr class="dropdown-divider"></li>
    <li>
      <a class="dropdown-item" href="#" (click)="printCurrentPaymentReceipt()">
        <i class="fa fa-print me-1"></i>
        Print Receipt
      </a>
    </li>
  </ul>
</div>

<!-- ADD PRINT PREVIEW MODAL -->
<app-print-preview
  [visible]="showPrintPreview"
  [previewData]="printPreviewData"
  (closePreview)="showPrintPreview = false"
  (print)="onPrintFromPreview($event)"
  (printed)="onPrintCompleted($event)">
</app-print-preview>
*/

// Module imports needed:
/*
// In the supplier-payment module or component imports:
import { PrintModule } from 'app/shared/modules/print';

// Or for standalone components:
import { 
  PrintPreviewComponent,
  PrintDirective,
  PrintFormatPipe 
} from 'app/shared/modules/print';
*/
