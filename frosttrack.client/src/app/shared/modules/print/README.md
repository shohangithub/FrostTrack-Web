# Print Module

A comprehensive, modular printing solution for Angular applications that supports multiple report types, templates, and output formats.

## Features

- 🖨️ **Multi-format printing**: HTML, PDF, and Image output
- 📋 **Multiple report types**: Receipts, Invoices, Purchase Orders, Statements, etc.
- 🎨 **Customizable templates**: Professional HTML templates with CSS styling
- ⚙️ **Configurable settings**: Branch-specific print configurations
- 📱 **Responsive design**: Support for different paper sizes including thermal printers
- 🔍 **Print preview**: Interactive preview with zoom and customization options
- 📦 **Modular architecture**: Easy to integrate and extend

## Installation

1. Import the PrintModule in your application:

```typescript
import { PrintModule } from "app/shared/modules/print";

@NgModule({
  imports: [PrintModule],
})
export class YourModule {}
```

## Quick Start

### 1. Basic Usage with Helper Service

```typescript
import { PrintHelperService, PrintReportType } from 'app/shared/modules/print';

constructor(private printHelper: PrintHelperService) {}

// Print a supplier payment receipt
async printSupplierPayment() {
  const paymentData = {
    id: 1,
    supplierName: 'ABC Supplier',
    paymentAmount: 1500.00,
    paymentMethod: 'Bank Transfer',
    paymentDate: new Date(),
    // ... other payment data
  };

  const result = await this.printHelper.printSupplierPayment(paymentData, 1);

  if (result.success) {
    console.log('Print successful!');
  } else {
    console.error('Print failed:', result.message);
  }
}
```

### 2. Using the Print Directive

```html
<!-- Simple print button -->
<button appPrint [printData]="invoiceData" [printType]="PrintReportType.SalesInvoice" [printFormat]="PrintFormat.HTML" (printCompleted)="onPrintSuccess($event)" (printFailed)="onPrintError($event)">Print Invoice</button>
```

### 3. Print Preview Modal

```typescript
// Component
showPreview = false;
previewData: IPrintPreviewData | null = null;

async openPrintPreview() {
  this.previewData = await this.printHelper.showPreview(
    this.reportData,
    PrintReportType.Invoice,
    this.branchId
  );
  this.showPreview = true;
}
```

```html
<!-- Template -->
<app-print-preview [visible]="showPreview" [previewData]="previewData" (closePreview)="showPreview = false" (printed)="onPrintCompleted($event)"> </app-print-preview>
```

## Supported Report Types

| Report Type       | Description                          | Template Available |
| ----------------- | ------------------------------------ | ------------------ |
| `SupplierPayment` | Payment receipts for suppliers       | ✅                 |
| `SalesInvoice`    | Sales invoices with itemized details | ✅                 |
| `PurchaseOrder`   | Purchase orders for suppliers        | ✅                 |
| `Receipt`         | Thermal printer receipts             | ✅                 |
| `Invoice`         | General invoices                     | ✅                 |
| `Statement`       | Account statements                   | ✅                 |
| `Voucher`         | Payment vouchers                     | ✅                 |
| `DeliveryNote`    | Delivery notes                       | ✅                 |
| `CreditNote`      | Credit notes                         | ✅                 |
| `DebitNote`       | Debit notes                          | ✅                 |

## Services

### PrintHelperService

Simplified service for common print operations:

```typescript
// Quick print methods
printSupplierPayment(paymentData, branchId);
printSalesInvoice(invoiceData, branchId);
printPurchaseOrder(orderData, branchId);
printReceipt(receiptData, branchId);

// Generate PDF
generatePDF(reportData, reportType, branchId);

// Show preview
showPreview(reportData, reportType, branchId);

// Bulk printing
bulkPrint(printRequests, branchId);
```

### PrintService

Core printing service with advanced features:

```typescript
// Main printing method
printReport(printable: IPrintable, format: PrintFormat): Promise<IPrintResult>

// Generate preview
generatePreview(printable: IPrintable): Promise<IPrintPreviewData>

// Settings management
getPrintSettings(branchId: number): Observable<IPrintSettings>
updatePrintSettings(settings: IPrintSettings): Observable<IPrintResult>
```

### PrintTemplateService

Template management service:

```typescript
// Template operations
getTemplate(templateName: string): IPrintTemplate | undefined
getDefaultTemplate(reportType: PrintReportType): IPrintTemplate | undefined
saveTemplate(template: IPrintTemplate): void
deleteTemplate(templateName: string): boolean

// Validation
validateTemplateData(data: any, reportType: PrintReportType)
getTemplateVariables(templateName: string): ITemplateVariable[]
```

### PrintConfigurationService

Configuration management service:

```typescript
// Configuration methods
getReportConfiguration(reportType: PrintReportType): Partial<IPrintSettings>
updateReportConfiguration(reportType: PrintReportType, settings: Partial<IPrintSettings>)
getMergedSettings(reportType: PrintReportType, branchSettings?: Partial<IPrintSettings>)

// Utilities
getAvailablePaperSizes()
getAvailableOrientations()
getAvailableFontSizes()
validateSettings(reportType: PrintReportType, settings: Partial<IPrintSettings>)
```

## Components

### PrintPreviewComponent

Interactive print preview modal with customization options:

```html
<app-print-preview [visible]="showPreview" [previewData]="previewData" [allowFormatChange]="true" [allowCustomSettings]="true" (closePreview)="onClose()" (print)="onPrint($event)" (printed)="onPrintCompleted($event)"> </app-print-preview>
```

### PrintHeaderComponent

Reusable header component for reports:

```html
<app-print-header [companyName]="settings.companyName" [companyAddress]="settings.companyAddress" [logoUrl]="settings.logoUrl" [showLogo]="settings.showLogo"> </app-print-header>
```

### PrintFooterComponent

Reusable footer component for reports:

```html
<app-print-footer [footerText]="settings.footerText" [termsAndConditions]="settings.termsAndConditions" [authorizedBy]="settings.authorizedBy" [showTerms]="settings.showTerms" [showSignature]="settings.showSignature"> </app-print-footer>
```

## Pipes

### PrintFormatPipe

Format values for printing:

```html
<!-- Currency formatting -->
{{ amount | printFormat:'currency':'USD':2 }}

<!-- Date formatting -->
{{ date | printFormat:'date':'dd/MM/yyyy' }}

<!-- Phone formatting -->
{{ phone | printFormat:'phone':'US' }}

<!-- Number formatting -->
{{ value | printFormat:'number':2 }}

<!-- Title case -->
{{ text | printFormat:'title' }}
```

## Configuration

### Branch-Specific Settings

Configure print settings for each branch:

```typescript
const printSettings: IPrintSettings = {
  companyName: "Your Company",
  companyAddress: "123 Business St, City, State",
  companyPhone: "+1 (555) 123-4567",
  logoUrl: "/assets/images/logo.png",
  paperSize: PaperSize.A4,
  orientation: PrintOrientation.Portrait,
  showLogo: true,
  showTerms: true,
  // ... other settings
};

await this.printService.updatePrintSettings(printSettings);
```

### Report-Specific Configuration

Customize settings for specific report types:

```typescript
this.configService.updateReportConfiguration(PrintReportType.Receipt, {
  paperSize: PaperSize.Thermal80mm,
  fontSize: FontSize.Small,
  marginTop: 5,
  marginBottom: 5,
});
```

## Templates

### Creating Custom Templates

Templates use HTML with Mustache-style variables:

```html
<!DOCTYPE html>
<html>
  <head>
    <style>
      {{styles}}
    </style>
  </head>
  <body>
    <div class="header">
      {{#showLogo}}
      <img src="{{logoUrl}}" alt="Logo" />
      {{/showLogo}}
      <h2>{{companyName}}</h2>
    </div>

    <div class="content">
      <h3>{{reportTitle}}</h3>
      <!-- Report content -->
    </div>

    <div class="footer">{{footerText}}</div>
  </body>
</html>
```

### Template Variables

Common variables available in templates:

- `{{companyName}}` - Company name
- `{{companyAddress}}` - Company address
- `{{logoUrl}}` - Company logo URL
- `{{reportTitle}}` - Report title
- `{{reportDate}}` - Report date
- `{{branchName}}` - Branch name
- `{{footerText}}` - Footer text
- `{{authorizedBy}}` - Authorized person

## Migration Guide

### From Old Print Service

See the complete migration example in `examples/supplier-payment-migration.example.ts`

1. Replace old imports:

```typescript
// OLD
import { PrintService } from "app/common/services/print.service";

// NEW
import { PrintHelperService } from "app/shared/modules/print";
```

2. Update methods:

```typescript
// OLD
await this.printService.printPaymentReceipt(receiptData, options);

// NEW
await this.printHelper.printSupplierPayment(paymentData, branchId);
```

3. Add preview functionality:

```typescript
// NEW
async showPreview() {
  this.previewData = await this.printHelper.showPreview(data, type, branchId);
  this.showPreview = true;
}
```

## Paper Sizes

Supported paper sizes:

- **A4**: 210 × 297 mm (default for documents)
- **A5**: 148 × 210 mm (smaller documents)
- **Letter**: 8.5 × 11 inches (US standard)
- **Legal**: 8.5 × 14 inches (US legal)
- **Thermal80mm**: 80mm thermal receipt (POS)
- **Thermal58mm**: 58mm thermal receipt (compact POS)

## Print Formats

Available output formats:

- **HTML**: Direct browser printing
- **PDF**: Download as PDF file
- **Image**: Download as PNG image

## Error Handling

All print operations return a result object:

```typescript
interface IPrintResult {
  success: boolean;
  message: string;
  data?: any;
  error?: string;
}
```

Handle errors appropriately:

```typescript
const result = await this.printHelper.printSupplierPayment(data, branchId);

if (result.success) {
  this.toastr.success(result.message);
} else {
  this.toastr.error(result.message);
  console.error("Print error:", result.error);
}
```

## Best Practices

1. **Use the Helper Service** for simple operations
2. **Configure branch settings** before first use
3. **Validate data** before printing
4. **Handle errors gracefully** with user feedback
5. **Use print preview** for complex documents
6. **Choose appropriate paper sizes** for content type
7. **Test on target printers** especially thermal printers

## API Integration

The module integrates with your backend API:

- `GET /api/Print/settings/{branchId}` - Get print settings
- `POST /api/Print/settings` - Update print settings
- `GET /api/Print/receipt/supplier-payment/{paymentId}` - Get receipt data

## Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Dependencies

- Angular 18+
- jsPDF
- html2canvas
- RxJS 7+

## License

This module is part of the FrostTrack application.
