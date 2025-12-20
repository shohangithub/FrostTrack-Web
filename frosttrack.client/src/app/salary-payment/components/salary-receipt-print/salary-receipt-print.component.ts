import { Component, OnInit, ViewChild, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TransactionReceiptPrintComponent } from '../../../transaction/components/transaction-receipt-print/transaction-receipt-print.component';
import { LayoutService } from '@core/service/layout.service';

@Component({
  selector: 'app-salary-receipt-print',
  templateUrl: './salary-receipt-print.component.html',
  styleUrls: ['./salary-receipt-print.component.scss'],
  standalone: true,
  imports: [CommonModule, TransactionReceiptPrintComponent],
})
export class SalaryReceiptPrintComponent implements OnInit {
  @ViewChild(TransactionReceiptPrintComponent)
  transactionReceiptComponent!: TransactionReceiptPrintComponent;

  @Input() preloadedTransactionId: string = '';
  @Input() hideSearchForm: boolean = false;

  transactionId: string = '';
  receiptTitle: string = 'বেতন প্রদান স্লিপ'; // Salary Payment Slip in Bengali
  backUrl: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private layoutService: LayoutService
  ) {
    this.layoutService.loadCurrentRoute();
  }

  ngOnInit(): void {
    // Check if transaction ID is passed via route params
    const id = this.route.snapshot.paramMap.get('id');
    const backurl = this.route.snapshot.paramMap.get('backurl');

    // Use Input property if provided, otherwise use route param
    if (this.preloadedTransactionId) {
      this.transactionId = this.preloadedTransactionId;
    } else if (id) {
      this.transactionId = id;
      this.hideSearchForm = true;
    }

    if (backurl) {
      this.backUrl = backurl;
    }
  }

  /**
   * Trigger print on the underlying TransactionReceiptPrintComponent
   * This method is called by parent components for inline printing
   */
  triggerPrint(): void {
    if (this.transactionReceiptComponent) {
      this.transactionReceiptComponent.triggerPrint();
    }
  }
}
