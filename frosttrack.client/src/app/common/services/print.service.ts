import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface IPrintHeader {
  companyName: string;
  companyAddress: string;
  companyPhone: string;
  companyEmail: string;
  companyWebsite?: string;
  logo?: string;
  branchName?: string;
  branchAddress?: string;
  branchPhone?: string;
}

export interface IPrintFooter {
  footerText: string;
  termsAndConditions?: string;
  thankYouMessage?: string;
  signature?: string;
  authorizedBy?: string;
}

export interface IPrintOptions {
  header: IPrintHeader;
  footer: IPrintFooter;
  showLogo: boolean;
  showBranchInfo: boolean;
  paperSize: 'A4' | 'A5' | 'RECEIPT' | 'THERMAL';
  orientation: 'portrait' | 'landscape';
  fontSize: 'small' | 'medium' | 'large';
  copies: number;
}

@Injectable({
  providedIn: 'root',
})
export class PrintService {
  private apiUrl = `${environment.apiUrl}/print`;

  constructor(private http: HttpClient) {}

  // Get print settings for a specific branch
  getPrintSettings(branchId: number): Observable<IPrintOptions> {
    return this.http.get<IPrintOptions>(`${this.apiUrl}/settings/${branchId}`);
  }

  // Print payment receipt
  printPaymentReceipt(
    paymentId: number,
    options?: Partial<IPrintOptions>
  ): void {
    this.generatePaymentReceipt(paymentId, options).subscribe((html) => {
      this.printHTML(html);
    });
  }

  // Generate payment receipt HTML
  generatePaymentReceipt(
    paymentId: number,
    options?: Partial<IPrintOptions>
  ): Observable<string> {
    return this.http.post<string>(
      `${this.apiUrl}/payment-receipt/${paymentId}`,
      options
    );
  }

  // Generic print function
  printHTML(htmlContent: string, options?: Partial<IPrintOptions>): void {
    const printWindow = window.open('', '_blank', 'width=800,height=600');

    if (!printWindow) {
      console.error(
        'Unable to open print window. Please check popup blocker settings.'
      );
      return;
    }

    const defaultOptions: IPrintOptions = {
      header: {
        companyName: '',
        companyAddress: '',
        companyPhone: '',
        companyEmail: '',
      },
      footer: {
        footerText: 'Thank you for your business!',
      },
      showLogo: true,
      showBranchInfo: true,
      paperSize: 'A4',
      orientation: 'portrait',
      fontSize: 'medium',
      copies: 1,
    };

    const finalOptions = { ...defaultOptions, ...options };

    const printContent = this.createPrintDocument(htmlContent, finalOptions);

    printWindow.document.write(printContent);
    printWindow.document.close();

    // Wait for content to load, then print
    printWindow.onload = () => {
      setTimeout(() => {
        printWindow.focus();
        printWindow.print();
        printWindow.close();
      }, 250);
    };
  }

  // Create complete print document with styles
  private createPrintDocument(content: string, options: IPrintOptions): string {
    const styles = this.getPrintStyles(options);

    return `
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset="utf-8">
          <title>Payment Receipt</title>
          <style>${styles}</style>
        </head>
        <body>
          ${content}
        </body>
      </html>
    `;
  }

  // Get print-specific CSS styles
  private getPrintStyles(options: IPrintOptions): string {
    const fontSizes = {
      small: '12px',
      medium: '14px',
      large: '16px',
    };

    const paperSizes = {
      A4: { width: '210mm', height: '297mm' },
      A5: { width: '148mm', height: '210mm' },
      RECEIPT: { width: '80mm', height: 'auto' },
      THERMAL: { width: '58mm', height: 'auto' },
    };

    const size = paperSizes[options.paperSize];

    return `
      @page {
        size: ${
          options.paperSize === 'A4' || options.paperSize === 'A5'
            ? `${size.width} ${size.height}`
            : `${size.width} ${size.height}`
        };
        margin: ${
          options.paperSize.includes('RECEIPT') ||
          options.paperSize === 'THERMAL'
            ? '5mm'
            : '15mm'
        };
        orientation: ${options.orientation};
      }

      body {
        font-family: 'Arial', sans-serif;
        font-size: ${fontSizes[options.fontSize]};
        line-height: 1.4;
        margin: 0;
        padding: 0;
        color: #333;
        ${
          options.paperSize.includes('RECEIPT') ||
          options.paperSize === 'THERMAL'
            ? 'width: 100%; max-width: ' + size.width + ';'
            : ''
        }
      }

      .print-container {
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
      }

      .print-header {
        text-align: center;
        border-bottom: 2px solid #333;
        padding-bottom: 10px;
        margin-bottom: 20px;
      }

      .print-header h1 {
        margin: 0;
        font-size: ${
          options.fontSize === 'large'
            ? '24px'
            : options.fontSize === 'medium'
            ? '20px'
            : '18px'
        };
        font-weight: bold;
      }

      .print-header .company-info {
        margin: 5px 0;
        font-size: ${
          options.fontSize === 'large'
            ? '14px'
            : options.fontSize === 'medium'
            ? '12px'
            : '10px'
        };
      }

      .print-content {
        flex: 1;
        padding: 10px 0;
      }

      .print-footer {
        border-top: 1px solid #333;
        padding-top: 10px;
        margin-top: 20px;
        text-align: center;
        font-size: ${
          options.fontSize === 'large'
            ? '12px'
            : options.fontSize === 'medium'
            ? '10px'
            : '8px'
        };
      }

      .receipt-table {
        width: 100%;
        border-collapse: collapse;
        margin: 15px 0;
      }

      .receipt-table th,
      .receipt-table td {
        padding: 8px;
        text-align: left;
        border-bottom: 1px solid #ddd;
      }

      .receipt-table th {
        background-color: #f8f9fa;
        font-weight: bold;
      }

      .amount-row {
        font-weight: bold;
        background-color: #f8f9fa;
      }

      .text-right {
        text-align: right;
      }

      .text-center {
        text-align: center;
      }

      .mb-2 {
        margin-bottom: 10px;
      }

      .mb-3 {
        margin-bottom: 15px;
      }

      .row {
        display: flex;
        justify-content: space-between;
        margin-bottom: 5px;
      }

      .col-label {
        font-weight: bold;
        width: 40%;
      }

      .col-value {
        width: 60%;
      }

      @media print {
        body {
          -webkit-print-color-adjust: exact;
          print-color-adjust: exact;
        }
        
        .no-print {
          display: none !important;
        }
      }
    `;
  }

  // Print preview functionality
  showPrintPreview(
    htmlContent: string,
    options?: Partial<IPrintOptions>
  ): void {
    const previewWindow = window.open('', '_blank', 'width=900,height=700');

    if (!previewWindow) {
      console.error(
        'Unable to open preview window. Please check popup blocker settings.'
      );
      return;
    }

    const finalOptions = {
      paperSize: 'A4' as const,
      orientation: 'portrait' as const,
      fontSize: 'medium' as const,
      copies: 1,
      showLogo: true,
      showBranchInfo: true,
      header: {
        companyName: '',
        companyAddress: '',
        companyPhone: '',
        companyEmail: '',
      },
      footer: {
        footerText: 'Thank you for your business!',
      },
      ...options,
    };

    const previewContent = this.createPreviewDocument(
      htmlContent,
      finalOptions
    );

    previewWindow.document.write(previewContent);
    previewWindow.document.close();
  }

  private createPreviewDocument(
    content: string,
    options: IPrintOptions
  ): string {
    const styles = this.getPrintStyles(options);

    return `
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset="utf-8">
          <title>Print Preview - Payment Receipt</title>
          <style>
            ${styles}
            body {
              background: #f5f5f5;
              padding: 20px;
            }
            .preview-container {
              background: white;
              box-shadow: 0 0 10px rgba(0,0,0,0.1);
              max-width: 800px;
              margin: 0 auto;
              padding: 20px;
            }
            .preview-toolbar {
              position: fixed;
              top: 10px;
              right: 10px;
              z-index: 1000;
            }
            .preview-toolbar button {
              margin: 0 5px;
              padding: 10px 15px;
              background: #007bff;
              color: white;
              border: none;
              border-radius: 4px;
              cursor: pointer;
            }
            .preview-toolbar button:hover {
              background: #0056b3;
            }
          </style>
        </head>
        <body>
          <div class="preview-toolbar no-print">
            <button onclick="window.print()">Print</button>
            <button onclick="window.close()">Close</button>
          </div>
          <div class="preview-container">
            ${content}
          </div>
        </body>
      </html>
    `;
  }
}
