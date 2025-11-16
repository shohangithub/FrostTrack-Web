// Base interfaces for print functionality
export interface IPrintable {
  id: string | number;
  type: PrintReportType;
  data: any;
  templateName?: string;
  customSettings?: Partial<IPrintSettings>;
}

export interface IPrintSettings {
  // Company Information
  companyName: string;
  companyAddress: string;
  companyPhone: string;
  companyEmail: string;
  companyWebsite: string;
  logoUrl: string;

  // Branch Information
  branchId: number;
  branchAddress: string;
  branchPhone: string;
  showLogo: boolean;
  showBranchInfo: boolean;

  // Layout Settings
  paperSize: PaperSize;
  orientation: PrintOrientation;
  fontSize: FontSize;
  defaultCopies: number;
  marginTop: number;
  marginBottom: number;
  marginLeft: number;
  marginRight: number;

  // Content Settings
  footerText: string;
  termsAndConditions: string;
  thankYouMessage: string;
  authorizedBy: string;
  signature: string;

  // Display Options
  showPaymentDetails: boolean;
  showSupplierInfo: boolean;
  showAmountSummary: boolean;
  showNotes: boolean;
  showSignature: boolean;
  showTerms: boolean;

  // Report Specific
  reportTitle: string;
  reportNumberPrefix: string;
  reportDateFormat: string;

  // Color Scheme
  primaryColor: string;
  secondaryColor: string;
  textColor: string;
  borderColor: string;
}

export interface IPrintTemplate {
  name: string;
  type: PrintReportType;
  htmlTemplate: string;
  cssStyles: string;
  variables: string[];
  isDefault: boolean;
  description: string;
}

export interface IPrintResult {
  success: boolean;
  message: string;
  data?: any;
  error?: string;
}

export interface IPrintPreviewData {
  htmlContent: string;
  settings: IPrintSettings;
  reportType: PrintReportType;
  title: string;
}

// Enums
export enum PrintReportType {
  Receipt = 'receipt',
  Invoice = 'invoice',
  Statement = 'statement',
  Report = 'report',
  Voucher = 'voucher',
  SupplierPayment = 'supplier-payment',
  SalesInvoice = 'sales-invoice',
  PurchaseOrder = 'purchase-order',
  DeliveryNote = 'delivery-note',
  CreditNote = 'credit-note',
  DebitNote = 'debit-note',
  PaymentReceipt = 'payment-receipt',
  BankReconciliation = 'bank-reconciliation',
  InventoryReport = 'inventory-report',
  CustomReport = 'custom-report',
}

export enum PaperSize {
  A4 = 'A4',
  A5 = 'A5',
  Letter = 'Letter',
  Legal = 'Legal',
  Thermal80mm = 'Thermal80mm',
  Thermal58mm = 'Thermal58mm',
  Custom = 'Custom',
}

export enum PrintOrientation {
  Portrait = 'portrait',
  Landscape = 'landscape',
}

export enum FontSize {
  Small = 'small',
  Medium = 'medium',
  Large = 'large',
  ExtraLarge = 'extra-large',
}

export enum PrintFormat {
  PDF = 'pdf',
  HTML = 'html',
  Image = 'image',
}

// Template Variables Interface
export interface ITemplateVariable {
  name: string;
  type: 'string' | 'number' | 'date' | 'boolean' | 'array' | 'object';
  required: boolean;
  defaultValue?: any;
  description: string;
}

// Print Job Interface
export interface IPrintJob {
  id: string;
  reportType: PrintReportType;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  data: any;
  settings: IPrintSettings;
  template: string;
  createdAt: Date;
  completedAt?: Date;
  error?: string;
}

// Configuration for different report types
export interface IReportTypeConfig {
  type: PrintReportType;
  displayName: string;
  defaultTemplate: string;
  availableTemplates: string[];
  requiredFields: string[];
  optionalFields: string[];
  defaultSettings: Partial<IPrintSettings>;
}
