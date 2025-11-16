import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

import {
  IPrintable,
  IPrintSettings,
  IPrintResult,
  IPrintPreviewData,
  PrintReportType,
  PrintFormat,
  PaperSize,
  PrintOrientation,
} from '../interfaces/print.interfaces';

@Injectable({
  providedIn: 'root',
})
export class PrintService {
  private readonly apiUrl = '/api/Print';
  private defaultSettings!: IPrintSettings;
  private printSettingsCache = new Map<number, IPrintSettings>();

  // Observable for print job status
  private printJobSubject = new BehaviorSubject<string>('idle');
  public printJob$ = this.printJobSubject.asObservable();

  constructor(private http: HttpClient) {
    this.initializeDefaultSettings();
  }

  private initializeDefaultSettings(): void {
    this.defaultSettings = {
      companyName: 'Your Company Name',
      companyAddress: 'Your Company Address',
      companyPhone: '+1 (555) 123-4567',
      companyEmail: 'info@company.com',
      companyWebsite: 'www.company.com',
      logoUrl: '/assets/images/logo.png',
      branchId: 0,
      branchAddress: '',
      branchPhone: '',
      showLogo: true,
      showBranchInfo: true,
      paperSize: PaperSize.A4,
      orientation: PrintOrientation.Portrait,
      fontSize: 'medium' as any,
      defaultCopies: 1,
      marginTop: 20,
      marginBottom: 20,
      marginLeft: 20,
      marginRight: 20,
      footerText: 'Thank you for your business!',
      termsAndConditions: 'Terms and conditions apply.',
      thankYouMessage: 'Thank you!',
      authorizedBy: 'Manager',
      signature: '',
      showPaymentDetails: true,
      showSupplierInfo: true,
      showAmountSummary: true,
      showNotes: true,
      showSignature: true,
      showTerms: true,
      reportTitle: 'Report',
      reportNumberPrefix: 'RPT',
      reportDateFormat: 'dd/MM/yyyy',
      primaryColor: '#007bff',
      secondaryColor: '#6c757d',
      textColor: '#212529',
      borderColor: '#dee2e6',
    };
  }

  /**
   * Get print settings for a specific branch
   */
  getPrintSettings(branchId: number): Observable<IPrintSettings> {
    // Check cache first
    if (this.printSettingsCache.has(branchId)) {
      return new Observable((observer) => {
        observer.next(this.printSettingsCache.get(branchId)!);
        observer.complete();
      });
    }

    return this.http.get<IPrintSettings>(`${this.apiUrl}/settings/${branchId}`).pipe(
      map((settings: any) => {
        const printSettings = settings as IPrintSettings;
        // Cache the settings
        this.printSettingsCache.set(branchId, printSettings);
        return printSettings;
      }),
      catchError(() => {
        // Return default settings if API call fails
        return of({ ...this.defaultSettings, branchId });
      })
    );
  }

  /**
   * Update print settings for a branch
   */
  updatePrintSettings(settings: IPrintSettings): Observable<IPrintResult> {
    return this.http
      .post<IPrintResult>(`${this.apiUrl}/settings`, settings)
      .pipe(
        map((result) => {
          if (result.success) {
            // Update cache
            this.printSettingsCache.set(settings.branchId, settings);
          }
          return result;
        })
      );
  }

  /**
   * Generate and print a report
   */
  async printReport(
    printable: IPrintable,
    format: PrintFormat = PrintFormat.HTML
  ): Promise<IPrintResult> {
    try {
      this.printJobSubject.next('processing');

      const settings = await this.getPrintSettings(
        printable.data.branchId || 1
      ).toPromise();
      const mergedSettings = {
        ...settings,
        ...printable.customSettings,
      } as IPrintSettings;

      const htmlContent = await this.generateReportHtml(
        printable,
        mergedSettings
      );

      switch (format) {
        case PrintFormat.PDF:
          return await this.printAsPdf(htmlContent, mergedSettings);
        case PrintFormat.HTML:
          return await this.printAsHtml(htmlContent, mergedSettings);
        case PrintFormat.Image:
          return await this.printAsImage(htmlContent, mergedSettings);
        default:
          throw new Error(`Unsupported print format: ${format}`);
      }
    } catch (error) {
      this.printJobSubject.next('failed');
      return {
        success: false,
        message: 'Print job failed',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    } finally {
      this.printJobSubject.next('idle');
    }
  }

  /**
   * Generate preview data for a report
   */
  async generatePreview(printable: IPrintable): Promise<IPrintPreviewData> {
    const settings = await this.getPrintSettings(
      printable.data.branchId || 1
    ).toPromise();
    const mergedSettings = {
      ...settings,
      ...printable.customSettings,
    } as IPrintSettings;

    const htmlContent = await this.generateReportHtml(
      printable,
      mergedSettings
    );

    return {
      htmlContent,
      settings: mergedSettings,
      reportType: printable.type,
      title: `${printable.type} Preview`,
    };
  }

  /**
   * Generate HTML content for a report
   */
  private async generateReportHtml(
    printable: IPrintable,
    settings: IPrintSettings
  ): Promise<string> {
    const templateName =
      printable.templateName || this.getDefaultTemplate(printable.type);

    // Get template content
    const template = await this.getTemplate(templateName, printable.type);

    // Replace variables in template
    return this.replaceTemplateVariables(template, printable.data, settings);
  }

  /**
   * Get template for report type
   */
  private async getTemplate(
    templateName: string,
    reportType: PrintReportType
  ): Promise<string> {
    // For now, return a basic template - this will be extended with template service
    return this.getBasicTemplate(reportType);
  }

  /**
   * Get default template for report type
   */
  private getDefaultTemplate(reportType: PrintReportType): string {
    const templateMap: Record<PrintReportType, string> = {
      [PrintReportType.Receipt]: 'receipt-default',
      [PrintReportType.Invoice]: 'invoice-default',
      [PrintReportType.Statement]: 'statement-default',
      [PrintReportType.Report]: 'report-default',
      [PrintReportType.Voucher]: 'voucher-default',
      [PrintReportType.SupplierPayment]: 'supplier-payment-default',
      [PrintReportType.SalesInvoice]: 'sales-invoice-default',
      [PrintReportType.PurchaseOrder]: 'purchase-order-default',
      [PrintReportType.DeliveryNote]: 'delivery-note-default',
      [PrintReportType.CreditNote]: 'credit-note-default',
      [PrintReportType.DebitNote]: 'debit-note-default',
      [PrintReportType.PaymentReceipt]: 'payment-receipt-default',
      [PrintReportType.BankReconciliation]: 'bank-reconciliation-default',
      [PrintReportType.InventoryReport]: 'inventory-report-default',
      [PrintReportType.CustomReport]: 'custom-report-default',
    };

    return templateMap[reportType] || 'report-default';
  }

  /**
   * Get basic template for report type
   */
  private getBasicTemplate(_reportType: PrintReportType): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>{{reportTitle}}</title>
        <style>
          body { 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: {{marginTop}}px {{marginRight}}px {{marginBottom}}px {{marginLeft}}px;
            color: {{textColor}};
            font-size: {{fontSize}};
          }
          .header { 
            text-align: center; 
            border-bottom: 2px solid {{borderColor}}; 
            padding-bottom: 20px; 
            margin-bottom: 30px; 
          }
          .company-logo { max-height: 80px; margin-bottom: 10px; }
          .company-info { margin-bottom: 20px; }
          .report-title { 
            font-size: 24px; 
            font-weight: bold; 
            color: {{primaryColor}}; 
            margin: 20px 0; 
          }
          .content { margin: 20px 0; }
          .footer { 
            border-top: 1px solid {{borderColor}}; 
            padding-top: 20px; 
            margin-top: 30px; 
            text-align: center; 
            font-size: 12px; 
            color: {{secondaryColor}};
          }
          table { width: 100%; border-collapse: collapse; margin: 20px 0; }
          th, td { padding: 8px; text-align: left; border-bottom: 1px solid {{borderColor}}; }
          th { background-color: #f8f9fa; font-weight: bold; }
          .amount { text-align: right; font-weight: bold; }
          .total-row { font-weight: bold; background-color: #f8f9fa; }
        </style>
      </head>
      <body>
        {{#showLogo}}
        <div class="header">
          <img src="{{logoUrl}}" alt="Company Logo" class="company-logo">
          <div class="company-info">
            <h2>{{companyName}}</h2>
            <p>{{companyAddress}}</p>
            <p>Phone: {{companyPhone}} | Email: {{companyEmail}}</p>
            <p>Website: {{companyWebsite}}</p>
          </div>
        </div>
        {{/showLogo}}
        
        <div class="report-title">{{reportTitle}}</div>
        
        <div class="content">
          {{content}}
        </div>
        
        <div class="footer">
          {{#showTerms}}
          <p>{{termsAndConditions}}</p>
          {{/showTerms}}
          <p>{{footerText}}</p>
          {{#showSignature}}
          <p>Authorized by: {{authorizedBy}}</p>
          {{/showSignature}}
        </div>
      </body>
      </html>
    `;
  }

  /**
   * Replace template variables with actual data
   */
  private replaceTemplateVariables(
    template: string,
    data: any,
    settings: IPrintSettings
  ): string {
    let html = template;

    // Replace settings variables
    Object.keys(settings).forEach((key) => {
      const value = (settings as any)[key];
      const regex = new RegExp(`{{${key}}}`, 'g');
      html = html.replace(regex, value?.toString() || '');
    });

    // Replace data variables
    Object.keys(data).forEach((key) => {
      const value = data[key];
      const regex = new RegExp(`{{${key}}}`, 'g');
      html = html.replace(regex, value?.toString() || '');
    });

    // Handle conditional blocks (basic implementation)
    html = this.handleConditionalBlocks(html, { ...settings, ...data });

    return html;
  }

  /**
   * Handle conditional blocks in templates
   */
  private handleConditionalBlocks(html: string, data: any): string {
    // Handle {{#condition}} ... {{/condition}} blocks
    const conditionalRegex = /{{#(\w+)}}([\s\S]*?){{\/\1}}/g;

    return html.replace(conditionalRegex, (match, condition, content) => {
      const value = data[condition];
      return value ? content : '';
    });
  }

  /**
   * Print as HTML
   */
  private async printAsHtml(
    htmlContent: string,
    _settings: IPrintSettings
  ): Promise<IPrintResult> {
    try {
      const printWindow = window.open('', '_blank');
      if (!printWindow) {
        throw new Error(
          'Could not open print window. Please check popup blocker settings.'
        );
      }

      printWindow.document.write(htmlContent);
      printWindow.document.close();

      // Wait for content to load then print
      printWindow.onload = () => {
        printWindow.print();
        setTimeout(() => printWindow.close(), 1000);
      };

      return {
        success: true,
        message: 'Document sent to printer successfully',
      };
    } catch (error) {
      return {
        success: false,
        message: 'Failed to print document',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Print as PDF
   */
  private async printAsPdf(
    htmlContent: string,
    settings: IPrintSettings
  ): Promise<IPrintResult> {
    try {
      // Create a temporary div to render the HTML
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = htmlContent;
      tempDiv.style.position = 'absolute';
      tempDiv.style.left = '-9999px';
      tempDiv.style.top = '0';
      tempDiv.style.width = '210mm'; // A4 width
      document.body.appendChild(tempDiv);

      // Convert to canvas
      const canvas = await html2canvas(tempDiv, {
        scale: 2,
        useCORS: true,
        allowTaint: true,
      });

      // Remove temporary div
      document.body.removeChild(tempDiv);

      // Create PDF
      const pdf = new jsPDF({
        orientation: settings.orientation === 'landscape' ? 'l' : 'p',
        unit: 'mm',
        format: settings.paperSize.toLowerCase(),
      });

      const imgData = canvas.toDataURL('image/png');
      const imgWidth = pdf.internal.pageSize.getWidth();
      const imgHeight = (canvas.height * imgWidth) / canvas.width;

      pdf.addImage(imgData, 'PNG', 0, 0, imgWidth, imgHeight);

      // Download PDF
      pdf.save(`${settings.reportTitle}_${new Date().getTime()}.pdf`);

      return {
        success: true,
        message: 'PDF generated and downloaded successfully',
      };
    } catch (error) {
      return {
        success: false,
        message: 'Failed to generate PDF',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Print as Image
   */
  private async printAsImage(
    htmlContent: string,
    settings: IPrintSettings
  ): Promise<IPrintResult> {
    try {
      // Create a temporary div to render the HTML
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = htmlContent;
      tempDiv.style.position = 'absolute';
      tempDiv.style.left = '-9999px';
      tempDiv.style.top = '0';
      tempDiv.style.width = '210mm'; // A4 width
      document.body.appendChild(tempDiv);

      // Convert to canvas
      const canvas = await html2canvas(tempDiv, {
        scale: 2,
        useCORS: true,
        allowTaint: true,
      });

      // Remove temporary div
      document.body.removeChild(tempDiv);

      // Download image
      const link = document.createElement('a');
      link.download = `${settings.reportTitle}_${new Date().getTime()}.png`;
      link.href = canvas.toDataURL();
      link.click();

      return {
        success: true,
        message: 'Image generated and downloaded successfully',
      };
    } catch (error) {
      return {
        success: false,
        message: 'Failed to generate image',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Clear print settings cache
   */
  clearCache(): void {
    this.printSettingsCache.clear();
  }

  /**
   * Get current print job status
   */
  getPrintJobStatus(): string {
    return this.printJobSubject.value;
  }
}
