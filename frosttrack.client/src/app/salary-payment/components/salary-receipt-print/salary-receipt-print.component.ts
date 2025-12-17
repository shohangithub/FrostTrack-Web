import { Component, OnInit } from '@angular/core';
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
  transactionId: string = '';
  receiptTitle: string = 'বেতন প্রদান স্লিপ'; // Salary Payment Slip in Bengali
  hideSearchForm: boolean = false;
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
    if (id) {
      this.transactionId = id;
      this.hideSearchForm = true;
    }
    if (backurl) {
      this.backUrl = backurl;
    }
  }
}
