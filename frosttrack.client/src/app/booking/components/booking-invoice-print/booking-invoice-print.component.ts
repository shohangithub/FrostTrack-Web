import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { environment } from 'environments/environment';

declare const JsBarcode: any;

@Component({
  selector: 'app-booking-invoice-print',
  templateUrl: './booking-invoice-print.component.html',
  styleUrls: ['./booking-invoice-print.component.scss'],
  standalone: true,
  imports: [CommonModule],
})
export class BookingInvoicePrintComponent implements OnInit {
  bookingId: string = '';
  invoiceHtml: SafeHtml = '';
  isLoading = true;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.bookingId = params['id'];
      this.loadInvoice();
    });
  }

  loadInvoice(): void {
    this.isLoading = true;
    const url = `${environment.apiUrl}/Print/booking-invoice/${this.bookingId}`;

    this.http.get(url, { responseType: 'text' }).subscribe({
      next: (html: string) => {
        this.invoiceHtml = this.sanitizer.bypassSecurityTrustHtml(html);
        this.isLoading = false;

        // Wait for DOM to render then generate barcode
        setTimeout(() => {
          this.generateBarcode();
        }, 500);
      },
      error: (error) => {
        console.error('Error loading invoice:', error);
        this.isLoading = false;
      },
    });
  }

  generateBarcode(): void {
    // Load JsBarcode script if not already loaded
    if (typeof JsBarcode === 'undefined') {
      const script = document.createElement('script');
      script.src =
        'https://cdn.jsdelivr.net/npm/jsbarcode@3.11.5/dist/JsBarcode.all.min.js';
      script.onload = () => {
        this.renderBarcode();
      };
      document.head.appendChild(script);
    } else {
      this.renderBarcode();
    }
  }

  renderBarcode(): void {
    const barcodeElement = document.querySelector(
      `[id^="barcode-"]`
    ) as SVGElement;
    if (barcodeElement && typeof JsBarcode !== 'undefined') {
      const bookingNumber = barcodeElement.id.replace('barcode-', '');
      JsBarcode(barcodeElement, bookingNumber, {
        format: 'CODE128',
        width: 2,
        height: 50,
        displayValue: false,
      });
    }
  }

  print(): void {
    window.print();
  }

  close(): void {
    window.close();
  }
}
