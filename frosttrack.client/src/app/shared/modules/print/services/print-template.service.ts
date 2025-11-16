import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

import {
  IPrintTemplate,
  PrintReportType,
  ITemplateVariable,
  IReportTypeConfig,
} from '../interfaces/print.interfaces';

@Injectable({
  providedIn: 'root',
})
export class PrintTemplateService {
  private templates = new Map<string, IPrintTemplate>();
  private reportConfigs = new Map<PrintReportType, IReportTypeConfig>();
  private templatesSubject = new BehaviorSubject<IPrintTemplate[]>([]);

  public templates$ = this.templatesSubject.asObservable();

  constructor() {
    this.initializeDefaultTemplates();
    this.initializeReportConfigs();
  }

  /**
   * Initialize default templates for different report types
   */
  private initializeDefaultTemplates(): void {
    // Supplier Payment Receipt Template
    const supplierPaymentTemplate: IPrintTemplate = {
      name: 'supplier-payment-default',
      type: PrintReportType.SupplierPayment,
      description: 'Default template for supplier payment receipts',
      isDefault: true,
      variables: [
        'companyName',
        'companyAddress',
        'companyPhone',
        'companyEmail',
        'supplierName',
        'supplierAddress',
        'paymentAmount',
        'paymentMethod',
        'paymentDate',
        'referenceNumber',
        'notes',
      ],
      htmlTemplate: this.getSupplierPaymentTemplate(),
      cssStyles: this.getDefaultStyles(),
    };

    // Sales Invoice Template
    const salesInvoiceTemplate: IPrintTemplate = {
      name: 'sales-invoice-default',
      type: PrintReportType.SalesInvoice,
      description: 'Default template for sales invoices',
      isDefault: true,
      variables: [
        'companyName',
        'companyAddress',
        'invoiceNumber',
        'customerName',
        'salesItems',
        'totalAmount',
        'taxAmount',
        'grandTotal',
      ],
      htmlTemplate: this.getSalesInvoiceTemplate(),
      cssStyles: this.getDefaultStyles(),
    };

    // Purchase Order Template
    const purchaseOrderTemplate: IPrintTemplate = {
      name: 'purchase-order-default',
      type: PrintReportType.PurchaseOrder,
      description: 'Default template for purchase orders',
      isDefault: true,
      variables: [
        'companyName',
        'supplierName',
        'orderNumber',
        'orderDate',
        'purchaseItems',
        'totalAmount',
        'deliveryDate',
      ],
      htmlTemplate: this.getPurchaseOrderTemplate(),
      cssStyles: this.getDefaultStyles(),
    };

    // Receipt Template
    const receiptTemplate: IPrintTemplate = {
      name: 'receipt-default',
      type: PrintReportType.Receipt,
      description: 'Default template for receipts',
      isDefault: true,
      variables: [
        'companyName',
        'receiptNumber',
        'customerName',
        'items',
        'totalAmount',
        'paymentMethod',
        'changeAmount',
      ],
      htmlTemplate: this.getReceiptTemplate(),
      cssStyles: this.getDefaultStyles(),
    };

    // Store templates
    this.templates.set(supplierPaymentTemplate.name, supplierPaymentTemplate);
    this.templates.set(salesInvoiceTemplate.name, salesInvoiceTemplate);
    this.templates.set(purchaseOrderTemplate.name, purchaseOrderTemplate);
    this.templates.set(receiptTemplate.name, receiptTemplate);

    // Update observable
    this.updateTemplatesSubject();
  }

  /**
   * Initialize report type configurations
   */
  private initializeReportConfigs(): void {
    const configs: IReportTypeConfig[] = [
      {
        type: PrintReportType.SupplierPayment,
        displayName: 'Supplier Payment Receipt',
        defaultTemplate: 'supplier-payment-default',
        availableTemplates: ['supplier-payment-default'],
        requiredFields: ['supplierName', 'paymentAmount', 'paymentMethod'],
        optionalFields: ['notes', 'referenceNumber'],
        defaultSettings: {
          reportTitle: 'Payment Receipt',
          reportNumberPrefix: 'PR',
          showSupplierInfo: true,
          showPaymentDetails: true,
        },
      },
      {
        type: PrintReportType.SalesInvoice,
        displayName: 'Sales Invoice',
        defaultTemplate: 'sales-invoice-default',
        availableTemplates: ['sales-invoice-default'],
        requiredFields: ['customerName', 'salesItems', 'totalAmount'],
        optionalFields: ['taxAmount', 'discount'],
        defaultSettings: {
          reportTitle: 'Sales Invoice',
          reportNumberPrefix: 'INV',
          showAmountSummary: true,
        },
      },
      {
        type: PrintReportType.PurchaseOrder,
        displayName: 'Purchase Order',
        defaultTemplate: 'purchase-order-default',
        availableTemplates: ['purchase-order-default'],
        requiredFields: ['supplierName', 'purchaseItems', 'totalAmount'],
        optionalFields: ['deliveryDate', 'notes'],
        defaultSettings: {
          reportTitle: 'Purchase Order',
          reportNumberPrefix: 'PO',
          showSupplierInfo: true,
        },
      },
      {
        type: PrintReportType.Receipt,
        displayName: 'Receipt',
        defaultTemplate: 'receipt-default',
        availableTemplates: ['receipt-default'],
        requiredFields: ['items', 'totalAmount'],
        optionalFields: ['customerName', 'changeAmount'],
        defaultSettings: {
          reportTitle: 'Receipt',
          reportNumberPrefix: 'RCP',
          paperSize: 'Thermal80mm' as any,
        },
      },
    ];

    configs.forEach((config) => {
      this.reportConfigs.set(config.type, config);
    });
  }

  /**
   * Get template by name
   */
  getTemplate(templateName: string): IPrintTemplate | undefined {
    return this.templates.get(templateName);
  }

  /**
   * Get default template for report type
   */
  getDefaultTemplate(reportType: PrintReportType): IPrintTemplate | undefined {
    const config = this.reportConfigs.get(reportType);
    if (config) {
      return this.templates.get(config.defaultTemplate);
    }
    return undefined;
  }

  /**
   * Get all templates for a report type
   */
  getTemplatesForType(reportType: PrintReportType): IPrintTemplate[] {
    return Array.from(this.templates.values()).filter(
      (template) => template.type === reportType
    );
  }

  /**
   * Get all available templates
   */
  getAllTemplates(): IPrintTemplate[] {
    return Array.from(this.templates.values());
  }

  /**
   * Add or update a template
   */
  saveTemplate(template: IPrintTemplate): void {
    this.templates.set(template.name, template);
    this.updateTemplatesSubject();
  }

  /**
   * Delete a template
   */
  deleteTemplate(templateName: string): boolean {
    const template = this.templates.get(templateName);
    if (template && !template.isDefault) {
      this.templates.delete(templateName);
      this.updateTemplatesSubject();
      return true;
    }
    return false;
  }

  /**
   * Get report type configuration
   */
  getReportConfig(reportType: PrintReportType): IReportTypeConfig | undefined {
    return this.reportConfigs.get(reportType);
  }

  /**
   * Get template variables for a template
   */
  getTemplateVariables(templateName: string): ITemplateVariable[] {
    const template = this.templates.get(templateName);
    if (!template) return [];

    return template.variables.map((variable) => ({
      name: variable,
      type: this.inferVariableType(variable),
      required: this.isRequiredVariable(variable, template.type),
      description: this.getVariableDescription(variable),
    }));
  }

  /**
   * Validate template data against required fields
   */
  validateTemplateData(
    data: any,
    reportType: PrintReportType
  ): { isValid: boolean; missingFields: string[] } {
    const config = this.reportConfigs.get(reportType);
    if (!config) {
      return { isValid: true, missingFields: [] };
    }

    const missingFields = config.requiredFields.filter(
      (field) =>
        !(field in data) || data[field] === null || data[field] === undefined
    );

    return {
      isValid: missingFields.length === 0,
      missingFields,
    };
  }

  private updateTemplatesSubject(): void {
    this.templatesSubject.next(Array.from(this.templates.values()));
  }

  private inferVariableType(
    variable: string
  ): 'string' | 'number' | 'date' | 'boolean' | 'array' | 'object' {
    if (variable.toLowerCase().includes('date')) return 'date';
    if (
      variable.toLowerCase().includes('amount') ||
      variable.toLowerCase().includes('price')
    )
      return 'number';
    if (
      variable.toLowerCase().includes('items') ||
      variable.toLowerCase().includes('list')
    )
      return 'array';
    if (
      variable.toLowerCase().includes('show') ||
      variable.toLowerCase().includes('is')
    )
      return 'boolean';
    return 'string';
  }

  private isRequiredVariable(
    variable: string,
    reportType: PrintReportType
  ): boolean {
    const config = this.reportConfigs.get(reportType);
    return config ? config.requiredFields.includes(variable) : false;
  }

  private getVariableDescription(variable: string): string {
    const descriptions: { [key: string]: string } = {
      companyName: 'Company name',
      companyAddress: 'Company address',
      companyPhone: 'Company phone number',
      companyEmail: 'Company email address',
      supplierName: 'Supplier name',
      customerName: 'Customer name',
      paymentAmount: 'Payment amount',
      totalAmount: 'Total amount',
      paymentMethod: 'Payment method',
      paymentDate: 'Payment date',
      referenceNumber: 'Reference number',
      notes: 'Additional notes',
    };
    return descriptions[variable] || `${variable} value`;
  }

  private getSupplierPaymentTemplate(): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>{{reportTitle}}</title>
        <style>{{styles}}</style>
      </head>
      <body>
        {{#showLogo}}
        <div class="header">
          <img src="{{logoUrl}}" alt="Company Logo" class="company-logo">
          <div class="company-info">
            <h2>{{companyName}}</h2>
            <p>{{companyAddress}}</p>
            <p>Phone: {{companyPhone}} | Email: {{companyEmail}}</p>
          </div>
        </div>
        {{/showLogo}}
        
        <div class="report-title">PAYMENT RECEIPT</div>
        
        <div class="content">
          <table class="info-table">
            <tr>
              <td><strong>Receipt No:</strong></td>
              <td>{{receiptNumber}}</td>
              <td><strong>Date:</strong></td>
              <td>{{paymentDate}}</td>
            </tr>
            <tr>
              <td><strong>Supplier:</strong></td>
              <td colspan="3">{{supplierName}}</td>
            </tr>
          </table>

          <div class="payment-details">
            <h3>Payment Details</h3>
            <table class="payment-table">
              <tr>
                <td><strong>Payment Method:</strong></td>
                <td>{{paymentMethod}}</td>
              </tr>
              <tr>
                <td><strong>Amount Paid:</strong></td>
                <td class="amount">{{paymentAmount}}</td>
              </tr>
              {{#referenceNumber}}
              <tr>
                <td><strong>Reference:</strong></td>
                <td>{{referenceNumber}}</td>
              </tr>
              {{/referenceNumber}}
            </table>
          </div>

          {{#notes}}
          <div class="notes">
            <h3>Notes</h3>
            <p>{{notes}}</p>
          </div>
          {{/notes}}
        </div>
        
        <div class="footer">
          <p>{{footerText}}</p>
          {{#showSignature}}
          <div class="signature">
            <p>Authorized by: {{authorizedBy}}</p>
          </div>
          {{/showSignature}}
        </div>
      </body>
      </html>
    `;
  }

  private getSalesInvoiceTemplate(): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>Sales Invoice</title>
        <style>{{styles}}</style>
      </head>
      <body>
        <div class="header">
          <img src="{{logoUrl}}" alt="Company Logo" class="company-logo">
          <div class="company-info">
            <h2>{{companyName}}</h2>
            <p>{{companyAddress}}</p>
          </div>
        </div>
        
        <div class="report-title">SALES INVOICE</div>
        
        <div class="invoice-info">
          <table>
            <tr>
              <td><strong>Invoice No:</strong> {{invoiceNumber}}</td>
              <td><strong>Date:</strong> {{invoiceDate}}</td>
            </tr>
            <tr>
              <td><strong>Customer:</strong> {{customerName}}</td>
              <td><strong>Due Date:</strong> {{dueDate}}</td>
            </tr>
          </table>
        </div>

        <div class="items">
          <table class="items-table">
            <thead>
              <tr>
                <th>Description</th>
                <th>Qty</th>
                <th>Price</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {{#salesItems}}
              <tr>
                <td>{{description}}</td>
                <td>{{quantity}}</td>
                <td>{{price}}</td>
                <td>{{total}}</td>
              </tr>
              {{/salesItems}}
            </tbody>
          </table>
        </div>

        <div class="totals">
          <table class="totals-table">
            <tr>
              <td>Subtotal:</td>
              <td>{{subtotal}}</td>
            </tr>
            <tr>
              <td>Tax:</td>
              <td>{{taxAmount}}</td>
            </tr>
            <tr class="total-row">
              <td><strong>Total:</strong></td>
              <td><strong>{{grandTotal}}</strong></td>
            </tr>
          </table>
        </div>
      </body>
      </html>
    `;
  }

  private getPurchaseOrderTemplate(): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>Purchase Order</title>
        <style>{{styles}}</style>
      </head>
      <body>
        <div class="header">
          <h2>{{companyName}}</h2>
          <p>{{companyAddress}}</p>
        </div>
        
        <div class="report-title">PURCHASE ORDER</div>
        
        <div class="order-info">
          <table>
            <tr>
              <td><strong>PO Number:</strong> {{orderNumber}}</td>
              <td><strong>Date:</strong> {{orderDate}}</td>
            </tr>
            <tr>
              <td><strong>Supplier:</strong> {{supplierName}}</td>
              <td><strong>Delivery Date:</strong> {{deliveryDate}}</td>
            </tr>
          </table>
        </div>

        <div class="items">
          <table class="items-table">
            <thead>
              <tr>
                <th>Item</th>
                <th>Quantity</th>
                <th>Unit Price</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              {{#purchaseItems}}
              <tr>
                <td>{{description}}</td>
                <td>{{quantity}}</td>
                <td>{{unitPrice}}</td>
                <td>{{total}}</td>
              </tr>
              {{/purchaseItems}}
            </tbody>
          </table>
        </div>

        <div class="total">
          <p><strong>Total Amount: {{totalAmount}}</strong></p>
        </div>
      </body>
      </html>
    `;
  }

  private getReceiptTemplate(): string {
    return `
      <!DOCTYPE html>
      <html>
      <head>
        <meta charset="utf-8">
        <title>Receipt</title>
        <style>
          body { font-family: monospace; width: 80mm; margin: 0; padding: 10px; }
          .center { text-align: center; }
          .receipt-title { font-size: 18px; font-weight: bold; }
          .item-line { display: flex; justify-content: space-between; }
          .total-line { border-top: 1px dashed #000; margin-top: 10px; padding-top: 5px; }
        </style>
      </head>
      <body>
        <div class="center">
          <div class="receipt-title">{{companyName}}</div>
          <p>{{companyAddress}}</p>
          <p>{{companyPhone}}</p>
        </div>
        
        <hr>
        
        <p><strong>Receipt #:</strong> {{receiptNumber}}</p>
        <p><strong>Date:</strong> {{saleDate}}</p>
        {{#customerName}}
        <p><strong>Customer:</strong> {{customerName}}</p>
        {{/customerName}}
        
        <hr>
        
        <div class="items">
          {{#items}}
          <div class="item-line">
            <span>{{name}}</span>
            <span>{{total}}</span>
          </div>
          <div style="font-size: 12px; color: #666;">
            {{quantity}} x {{price}}
          </div>
          {{/items}}
        </div>
        
        <div class="total-line">
          <div class="item-line">
            <strong>TOTAL:</strong>
            <strong>{{totalAmount}}</strong>
          </div>
          <div class="item-line">
            <span>Payment:</span>
            <span>{{paymentMethod}}</span>
          </div>
          {{#changeAmount}}
          <div class="item-line">
            <span>Change:</span>
            <span>{{changeAmount}}</span>
          </div>
          {{/changeAmount}}
        </div>
        
        <div class="center" style="margin-top: 20px;">
          <p>{{thankYouMessage}}</p>
        </div>
      </body>
      </html>
    `;
  }

  private getDefaultStyles(): string {
    return `
      body {
        font-family: Arial, sans-serif;
        margin: 0;
        padding: 20px;
        color: #333;
        line-height: 1.6;
      }
      .header {
        text-align: center;
        border-bottom: 2px solid #007bff;
        padding-bottom: 20px;
        margin-bottom: 30px;
      }
      .company-logo {
        max-height: 80px;
        margin-bottom: 10px;
      }
      .company-info h2 {
        margin: 0;
        color: #007bff;
      }
      .report-title {
        font-size: 24px;
        font-weight: bold;
        text-align: center;
        color: #007bff;
        margin: 30px 0;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        margin: 20px 0;
      }
      th, td {
        padding: 8px;
        text-align: left;
        border-bottom: 1px solid #ddd;
      }
      th {
        background-color: #f8f9fa;
        font-weight: bold;
      }
      .info-table, .payment-table {
        border: 1px solid #ddd;
      }
      .info-table td, .payment-table td {
        padding: 10px;
        border: 1px solid #ddd;
      }
      .amount {
        text-align: right;
        font-weight: bold;
        font-size: 18px;
      }
      .items-table {
        border: 1px solid #ddd;
      }
      .items-table th {
        background-color: #007bff;
        color: white;
      }
      .totals-table {
        margin-left: auto;
        width: 300px;
      }
      .total-row {
        font-weight: bold;
        font-size: 18px;
        background-color: #f8f9fa;
      }
      .footer {
        border-top: 1px solid #ddd;
        padding-top: 20px;
        margin-top: 30px;
        text-align: center;
        font-size: 12px;
        color: #666;
      }
      .signature {
        margin-top: 40px;
        text-align: right;
      }
      .notes {
        margin: 20px 0;
        padding: 15px;
        background-color: #f8f9fa;
        border-left: 4px solid #007bff;
      }
      .payment-details {
        margin: 20px 0;
      }
      .payment-details h3 {
        color: #007bff;
        border-bottom: 1px solid #ddd;
        padding-bottom: 5px;
      }
    `;
  }
}
