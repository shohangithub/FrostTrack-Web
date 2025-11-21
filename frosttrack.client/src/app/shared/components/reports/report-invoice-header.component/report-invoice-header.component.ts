import { Component, Input, OnInit } from '@angular/core';
import { BarcodeComponent } from '@shared/components/barcode/barcode.component';
@Component({
  selector: 'app-report-invoice-header',
  standalone: true,
  templateUrl: './report-invoice-header.component.html',
  styleUrls: ['./report-invoice-header.component.scss'],
  imports: [BarcodeComponent],
})
export class ReportInvoiceHeaderComponent implements OnInit {
  @Input() InvoiceNumber: string = '';
  @Input() Title: string = '';
  constructor() {}

  ngOnInit() {}
}
