import { Injectable } from '@angular/core';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import { ISalesResponse } from '../models/sales.interface';

@Injectable({
  providedIn: 'root',
})
export class InvoiceService {
  constructor() {}

  generateInvoice(salesData: ISalesResponse): void {
    const doc = new jsPDF();

    // Company Header
    doc.setFontSize(20);
    doc.setFont('helvetica', 'bold');
    doc.text('SALES INVOICE', 105, 20, { align: 'center' });

    // Company Information (You can customize this)
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text('Your Company Name', 20, 40);
    doc.text('Company Address Line 1', 20, 50);
    doc.text('Company Address Line 2', 20, 60);
    doc.text('Phone: +1234567890', 20, 70);
    doc.text('Email: info@company.com', 20, 80);

    // Invoice Information
    doc.setFont('helvetica', 'bold');
    doc.text('Invoice Details:', 120, 40);
    doc.setFont('helvetica', 'normal');
    doc.text(`Invoice No: ${salesData.invoiceNumber}`, 120, 50);
    doc.text(
      `Date: ${new Date(salesData.invoiceDate).toLocaleDateString()}`,
      120,
      60
    );
    doc.text(`Sales Type: ${salesData.salesType}`, 120, 70);

    // Customer Information
    doc.setFont('helvetica', 'bold');
    doc.text('Bill To:', 20, 100);
    doc.setFont('helvetica', 'normal');
    doc.text(
      `Customer: ${salesData.customer?.customerName || 'Walk-in Customer'}`,
      20,
      110
    );
    if (salesData.customer?.customerMobile) {
      doc.text(`Mobile: ${salesData.customer.customerMobile}`, 20, 120);
    }
    if (salesData.customer?.officePhone) {
      doc.text(`Phone: ${salesData.customer.officePhone}`, 20, 130);
    }
    if (salesData.customer?.address) {
      doc.text(`Address: ${salesData.customer.address}`, 20, 140);
    }

    // Prepare table data
    const tableData =
      salesData.salesDetails?.map((item, index) => [
        index + 1,
        item.product?.productName || '',
        item.salesQuantity?.toFixed(2) || '0',
        item.salesRate?.toFixed(2) || '0.00',
        item.salesAmount?.toFixed(2) || '0.00',
      ]) || [];

    // Product Table
    autoTable(doc, {
      startY: 150,
      head: [['S/N', 'Product Name', 'Quantity', 'Rate', 'Amount']],
      body: tableData,
      theme: 'striped',
      headStyles: {
        fillColor: [63, 81, 181],
        textColor: 255,
        fontSize: 10,
        fontStyle: 'bold',
      },
      bodyStyles: {
        fontSize: 9,
      },
      columnStyles: {
        0: { halign: 'center', cellWidth: 15 },
        1: { halign: 'left', cellWidth: 80 },
        2: { halign: 'right', cellWidth: 25 },
        3: { halign: 'right', cellWidth: 25 },
        4: { halign: 'right', cellWidth: 30 },
      },
      foot: [
        ['', '', '', 'Subtotal:', salesData.subtotal?.toFixed(2) || '0.00'],
      ],
      footStyles: {
        fillColor: [240, 240, 240],
        textColor: 0,
        fontSize: 9,
        fontStyle: 'bold',
      },
    });

    // Calculate position for totals section
    const finalY = (doc as any).lastAutoTable.finalY + 10;

    // Totals Section
    const totalsY = finalY;
    const totalsX = 130;

    doc.setFont('helvetica', 'normal');
    doc.text(
      `Subtotal: $${salesData.subtotal?.toFixed(2) || '0.00'}`,
      totalsX,
      totalsY
    );

    if (salesData.vatPercent && salesData.vatPercent > 0) {
      doc.text(
        `VAT (${salesData.vatPercent}%): $${
          salesData.vatAmount?.toFixed(2) || '0.00'
        }`,
        totalsX,
        totalsY + 10
      );
    }

    if (salesData.discountPercent && salesData.discountPercent > 0) {
      doc.text(
        `Discount (${salesData.discountPercent}%): -$${
          salesData.discountAmount?.toFixed(2) || '0.00'
        }`,
        totalsX,
        totalsY + 20
      );
    }

    if (salesData.otherCost && salesData.otherCost > 0) {
      doc.text(
        `Other Costs: $${salesData.otherCost?.toFixed(2) || '0.00'}`,
        totalsX,
        totalsY + 30
      );
    }

    // Total line
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    const totalY = totalsY + 45;
    doc.text(
      `Total Amount: $${salesData.invoiceAmount?.toFixed(2) || '0.00'}`,
      totalsX,
      totalY
    );

    if (salesData.paidAmount && salesData.paidAmount > 0) {
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(10);
      doc.text(
        `Paid Amount: $${salesData.paidAmount?.toFixed(2) || '0.00'}`,
        totalsX,
        totalY + 10
      );

      const balance =
        (salesData.invoiceAmount || 0) - (salesData.paidAmount || 0);
      if (balance > 0) {
        doc.setFont('helvetica', 'bold');
        doc.text(`Balance Due: $${balance.toFixed(2)}`, totalsX, totalY + 20);
      }
    }

    // Footer
    const pageHeight = doc.internal.pageSize.height;
    doc.setFont('helvetica', 'italic');
    doc.setFontSize(8);
    doc.text('Thank you for your business!', 105, pageHeight - 30, {
      align: 'center',
    });
    doc.text('This is a computer generated invoice.', 105, pageHeight - 20, {
      align: 'center',
    });

    // Save/Print
    doc.save(`Invoice-${salesData.invoiceNumber}.pdf`);
  }

  printInvoice(salesData: ISalesResponse): void {
    const doc = new jsPDF();

    // Same content as generateInvoice but for printing
    // Company Header
    doc.setFontSize(20);
    doc.setFont('helvetica', 'bold');
    doc.text('SALES INVOICE', 105, 20, { align: 'center' });

    // Company Information
    doc.setFontSize(12);
    doc.setFont('helvetica', 'normal');
    doc.text('Your Company Name', 20, 40);
    doc.text('Company Address Line 1', 20, 50);
    doc.text('Company Address Line 2', 20, 60);
    doc.text('Phone: +1234567890', 20, 70);
    doc.text('Email: info@company.com', 20, 80);

    // Invoice Information
    doc.setFont('helvetica', 'bold');
    doc.text('Invoice Details:', 120, 40);
    doc.setFont('helvetica', 'normal');
    doc.text(`Invoice No: ${salesData.invoiceNumber}`, 120, 50);
    doc.text(
      `Date: ${new Date(salesData.invoiceDate).toLocaleDateString()}`,
      120,
      60
    );
    doc.text(`Sales Type: ${salesData.salesType}`, 120, 70);

    // Customer Information
    doc.setFont('helvetica', 'bold');
    doc.text('Bill To:', 20, 100);
    doc.setFont('helvetica', 'normal');
    doc.text(
      `Customer: ${salesData.customer?.customerName || 'Walk-in Customer'}`,
      20,
      110
    );
    if (salesData.customer?.customerMobile) {
      doc.text(`Mobile: ${salesData.customer.customerMobile}`, 20, 120);
    }
    if (salesData.customer?.officePhone) {
      doc.text(`Phone: ${salesData.customer.officePhone}`, 20, 130);
    }
    if (salesData.customer?.address) {
      doc.text(`Address: ${salesData.customer.address}`, 20, 140);
    }

    // Prepare table data
    const tableData =
      salesData.salesDetails?.map((item, index) => [
        index + 1,
        item.product?.productName || '',
        item.salesQuantity?.toFixed(2) || '0',
        item.salesRate?.toFixed(2) || '0.00',
        item.salesAmount?.toFixed(2) || '0.00',
      ]) || [];

    // Product Table
    autoTable(doc, {
      startY: 150,
      head: [['S/N', 'Product Name', 'Quantity', 'Rate', 'Amount']],
      body: tableData,
      theme: 'striped',
      headStyles: {
        fillColor: [63, 81, 181],
        textColor: 255,
        fontSize: 10,
        fontStyle: 'bold',
      },
      bodyStyles: {
        fontSize: 9,
      },
      columnStyles: {
        0: { halign: 'center', cellWidth: 15 },
        1: { halign: 'left', cellWidth: 80 },
        2: { halign: 'right', cellWidth: 25 },
        3: { halign: 'right', cellWidth: 25 },
        4: { halign: 'right', cellWidth: 30 },
      },
    });

    // Calculate position for totals section
    const finalY = (doc as any).lastAutoTable.finalY + 10;

    // Totals Section
    const totalsY = finalY;
    const totalsX = 130;

    doc.setFont('helvetica', 'normal');
    doc.text(
      `Subtotal: $${salesData.subtotal?.toFixed(2) || '0.00'}`,
      totalsX,
      totalsY
    );

    if (salesData.vatPercent && salesData.vatPercent > 0) {
      doc.text(
        `VAT (${salesData.vatPercent}%): $${
          salesData.vatAmount?.toFixed(2) || '0.00'
        }`,
        totalsX,
        totalsY + 10
      );
    }

    if (salesData.discountPercent && salesData.discountPercent > 0) {
      doc.text(
        `Discount (${salesData.discountPercent}%): -$${
          salesData.discountAmount?.toFixed(2) || '0.00'
        }`,
        totalsX,
        totalsY + 20
      );
    }

    if (salesData.otherCost && salesData.otherCost > 0) {
      doc.text(
        `Other Costs: $${salesData.otherCost?.toFixed(2) || '0.00'}`,
        totalsX,
        totalsY + 30
      );
    }

    // Total line
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    const totalY = totalsY + 45;
    doc.text(
      `Total Amount: $${salesData.invoiceAmount?.toFixed(2) || '0.00'}`,
      totalsX,
      totalY
    );

    if (salesData.paidAmount && salesData.paidAmount > 0) {
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(10);
      doc.text(
        `Paid Amount: $${salesData.paidAmount?.toFixed(2) || '0.00'}`,
        totalsX,
        totalY + 10
      );

      const balance =
        (salesData.invoiceAmount || 0) - (salesData.paidAmount || 0);
      if (balance > 0) {
        doc.setFont('helvetica', 'bold');
        doc.text(`Balance Due: $${balance.toFixed(2)}`, totalsX, totalY + 20);
      }
    }

    // Footer
    const pageHeight = doc.internal.pageSize.height;
    doc.setFont('helvetica', 'italic');
    doc.setFontSize(8);
    doc.text('Thank you for your business!', 105, pageHeight - 30, {
      align: 'center',
    });
    doc.text('This is a computer generated invoice.', 105, pageHeight - 20, {
      align: 'center',
    });

    // Open print dialog
    doc.autoPrint();
    window.open(doc.output('bloburl'), '_blank');
  }

  // Smart Print - Auto-detects best printing method
  smartPrint(
    salesData: ISalesResponse,
    printerType: 'pdf' | 'pos' | 'auto' = 'auto'
  ): void {
    switch (printerType) {
      case 'pdf':
        this.printInvoice(salesData);
        break;
      case 'pos':
        this.printToPOS(salesData);
        break;
      case 'auto':
        // Auto-detect based on device and available APIs
        if (this.shouldUsePOSPrint()) {
          this.printToPOS(salesData);
        } else {
          this.printInvoice(salesData);
        }
        break;
    }
  }

  // POS Printer Methods
  async printToPOS(salesData: ISalesResponse): Promise<void> {
    try {
      if ('serial' in navigator && (await this.hasSerialPermission())) {
        await this.printPOSReceipt(salesData);
      } else {
        this.printReceiptFormat(salesData);
      }
    } catch (error) {
      console.error('POS printing failed:', error);
      // Fallback to receipt format
      this.printReceiptFormat(salesData);
    }
  }

  private async printPOSReceipt(salesData: ISalesResponse): Promise<void> {
    try {
      // Request serial port access
      const port = await (navigator as any).serial.requestPort();
      await port.open({ baudRate: 9600 });

      const writer = port.writable.getWriter();

      // ESC/POS commands
      const ESC = '\x1B';
      const GS = '\x1D';

      // Initialize printer
      let commands = ESC + '@'; // Initialize

      // Center align and bold
      commands += ESC + 'a' + '\x01'; // Center
      commands += ESC + 'E' + '\x01'; // Bold on
      commands += 'SALES RECEIPT\n';
      commands += ESC + 'E' + '\x00'; // Bold off
      commands += ESC + 'a' + '\x00'; // Left align

      // Company info
      commands += '================================\n';
      commands += 'Your Company Name\n';
      commands += 'Company Address\n';
      commands += 'Phone: +1234567890\n';
      commands += '================================\n';

      // Invoice details
      commands += `Invoice: ${salesData.invoiceNumber}\n`;
      commands += `Date: ${new Date(
        salesData.invoiceDate
      ).toLocaleDateString()}\n`;
      commands += `Customer: ${
        salesData.customer?.customerName || 'Walk-in'
      }\n`;
      commands += '--------------------------------\n';

      // Items
      salesData.salesDetails?.forEach((item) => {
        const name = (item.product?.productName || '').substring(0, 20);
        const qty = item.salesQuantity?.toFixed(2) || '0';
        const rate = item.salesRate?.toFixed(2) || '0.00';
        const amount = item.salesAmount?.toFixed(2) || '0.00';

        commands += `${name}\n`;
        commands += `  ${qty} x $${rate} = $${amount}\n`;
      });

      commands += '--------------------------------\n';

      // Totals
      commands += `Subtotal:        $${
        salesData.subtotal?.toFixed(2) || '0.00'
      }\n`;

      if (salesData.vatAmount && salesData.vatAmount > 0) {
        commands += `VAT:             $${salesData.vatAmount.toFixed(2)}\n`;
      }

      if (salesData.discountAmount && salesData.discountAmount > 0) {
        commands += `Discount:       -$${salesData.discountAmount.toFixed(
          2
        )}\n`;
      }

      commands += '================================\n';

      // Bold total
      commands += ESC + 'E' + '\x01'; // Bold on
      commands += `TOTAL:           $${
        salesData.invoiceAmount?.toFixed(2) || '0.00'
      }\n`;
      commands += ESC + 'E' + '\x00'; // Bold off

      if (salesData.paidAmount && salesData.paidAmount > 0) {
        commands += `Paid:            $${salesData.paidAmount.toFixed(2)}\n`;
        const balance =
          (salesData.invoiceAmount || 0) - (salesData.paidAmount || 0);
        if (balance > 0) {
          commands += `Balance:         $${balance.toFixed(2)}\n`;
        }
      }

      commands += '================================\n';
      commands += ESC + 'a' + '\x01'; // Center
      commands += 'Thank you for your business!\n';
      commands += ESC + 'a' + '\x00'; // Left align

      // Cut paper
      commands += GS + 'V' + '\x41' + '\x03';

      // Send to printer
      const encoder = new TextEncoder();
      await writer.write(encoder.encode(commands));

      writer.releaseLock();
      await port.close();
    } catch (error) {
      console.error('Serial printing failed:', error);
      throw error;
    }
  }

  private printReceiptFormat(salesData: ISalesResponse): void {
    // Create receipt-style HTML for thermal-style printing
    const receiptWindow = window.open('', '_blank', 'width=320,height=600');

    if (receiptWindow) {
      receiptWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
          <title>Receipt - ${salesData.invoiceNumber}</title>
          <style>
            body {
              font-family: 'Courier New', monospace;
              font-size: 11px;
              width: 300px;
              margin: 0;
              padding: 10px;
              line-height: 1.2;
            }
            .center { text-align: center; }
            .bold { font-weight: bold; }
            .line { 
              border-top: 1px dashed #000; 
              margin: 8px 0; 
              width: 100%;
            }
            .double-line { 
              border-top: 2px solid #000; 
              margin: 8px 0; 
            }
            .item { 
              display: flex; 
              justify-content: space-between;
              margin: 2px 0;
            }
            .item-details {
              font-size: 9px;
              color: #666;
              margin-left: 10px;
              margin-bottom: 4px;
            }
            .total-line {
              display: flex;
              justify-content: space-between;
              font-weight: bold;
              font-size: 12px;
            }
            @media print {
              body { width: 80mm; }
              .no-print { display: none; }
            }
          </style>
        </head>
        <body>
          <div class="center bold" style="font-size: 14px;">SALES RECEIPT</div>
          <div class="line"></div>
          
          <div class="center">
            <div class="bold">Your Company Name</div>
            <div>Company Address Line 1</div>
            <div>Company Address Line 2</div>
            <div>Phone: +1234567890</div>
          </div>
          <div class="line"></div>
          
          <div><strong>Invoice:</strong> ${salesData.invoiceNumber}</div>
          <div><strong>Date:</strong> ${new Date(
            salesData.invoiceDate
          ).toLocaleDateString()}</div>
          <div><strong>Time:</strong> ${new Date().toLocaleTimeString()}</div>
          <div><strong>Customer:</strong> ${
            salesData.customer?.customerName || 'Walk-in Customer'
          }</div>
          ${
            salesData.customer?.customerMobile
              ? `<div><strong>Mobile:</strong> ${salesData.customer.customerMobile}</div>`
              : ''
          }
          
          <div class="line"></div>
          
          ${
            salesData.salesDetails
              ?.map(
                (item) => `
            <div class="item">
              <div style="width: 70%;">${item.product?.productName || ''}</div>
              <div style="width: 30%; text-align: right;">$${
                item.salesAmount?.toFixed(2) || '0.00'
              }</div>
            </div>
            <div class="item-details">
              ${item.salesQuantity?.toFixed(2) || '0'} x $${
                  item.salesRate?.toFixed(2) || '0.00'
                }
            </div>
          `
              )
              .join('') || ''
          }
          
          <div class="line"></div>
          
          <div class="item">
            <div>Subtotal:</div>
            <div>$${salesData.subtotal?.toFixed(2) || '0.00'}</div>
          </div>
          
          ${
            salesData.vatAmount && salesData.vatAmount > 0
              ? `
            <div class="item">
              <div>VAT (${salesData.vatPercent || 0}%):</div>
              <div>$${salesData.vatAmount.toFixed(2)}</div>
            </div>
          `
              : ''
          }
          
          ${
            salesData.discountAmount && salesData.discountAmount > 0
              ? `
            <div class="item">
              <div>Discount (${salesData.discountPercent || 0}%):</div>
              <div>-$${salesData.discountAmount.toFixed(2)}</div>
            </div>
          `
              : ''
          }
          
          ${
            salesData.otherCost && salesData.otherCost > 0
              ? `
            <div class="item">
              <div>Other Costs:</div>
              <div>$${salesData.otherCost.toFixed(2)}</div>
            </div>
          `
              : ''
          }
          
          <div class="double-line"></div>
          
          <div class="total-line">
            <div>TOTAL:</div>
            <div>$${salesData.invoiceAmount?.toFixed(2) || '0.00'}</div>
          </div>
          
          ${
            salesData.paidAmount && salesData.paidAmount > 0
              ? `
            <div class="item">
              <div>Paid:</div>
              <div>$${salesData.paidAmount.toFixed(2)}</div>
            </div>
            ${
              (salesData.invoiceAmount || 0) - (salesData.paidAmount || 0) > 0
                ? `
              <div class="item bold">
                <div>Balance Due:</div>
                <div>$${(
                  (salesData.invoiceAmount || 0) - (salesData.paidAmount || 0)
                ).toFixed(2)}</div>
              </div>
            `
                : ''
            }
          `
              : ''
          }
          
          <div class="line"></div>
          <div class="center">
            <div>Thank you for your business!</div>
            <div style="font-size: 9px; margin-top: 10px;">
              This is a computer generated receipt.
            </div>
          </div>
          
          <div class="no-print center" style="margin-top: 20px;">
            <button onclick="window.print()" style="padding: 10px 20px; font-size: 12px;">
              Print Receipt
            </button>
            <button onclick="window.close()" style="padding: 10px 20px; font-size: 12px; margin-left: 10px;">
              Close
            </button>
          </div>
          
          <script>
            // Auto-print for POS systems
            window.onload = function() {
              setTimeout(function() {
                window.print();
              }, 500);
            };
          </script>
        </body>
        </html>
      `);

      receiptWindow.document.close();
    }
  }

  // Detection Methods
  private shouldUsePOSPrint(): boolean {
    // Check multiple factors to determine if POS printing should be used
    return (
      this.isMobileOrTablet() ||
      this.isKioskMode() ||
      this.hasSerialSupport() ||
      this.isSmallScreen()
    );
  }

  private isMobileOrTablet(): boolean {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
      navigator.userAgent
    );
  }

  private isKioskMode(): boolean {
    // Check if running in kiosk mode (common for POS systems)
    return (
      (window.navigator as any).standalone ||
      window.matchMedia('(display-mode: fullscreen)').matches ||
      document.fullscreenElement !== null
    );
  }

  private hasSerialSupport(): boolean {
    return 'serial' in navigator;
  }

  private isSmallScreen(): boolean {
    return window.innerWidth <= 768 || window.screen.width <= 768;
  }

  private async hasSerialPermission(): Promise<boolean> {
    try {
      if ('serial' in navigator) {
        const ports = await (navigator as any).serial.getPorts();
        return ports.length > 0;
      }
      return false;
    } catch {
      return false;
    }
  }

  // Utility method to get recommended print type
  getRecommendedPrintType(): 'pdf' | 'pos' {
    return this.shouldUsePOSPrint() ? 'pos' : 'pdf';
  }

  // Method to test both print types
  testPrint(salesData: ISalesResponse): void {
    const printType = this.getRecommendedPrintType();
    console.log(`Recommended print type: ${printType}`);
    console.log('Device info:', {
      isMobile: this.isMobileOrTablet(),
      isKiosk: this.isKioskMode(),
      hasSerial: this.hasSerialSupport(),
      isSmallScreen: this.isSmallScreen(),
      screenWidth: window.innerWidth,
    });

    this.smartPrint(salesData, 'auto');
  }
}
