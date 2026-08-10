export interface ICompanyListResponse {
  id: number;
  name: string;
  businessCurrency: string;
  currencySymbol: string;
  codeGeneration: number;
  codeGenerationName: string;
  autoGenerateBookingNo: boolean;
  isActive: boolean;
  status: string;
}

export interface ICompanyResponse {
  id: number;
  name: string;
  logoUrl: string;
  businessCurrency: string;
  currencySymbol: string;
  description: string;
  autoInvoicePrint: boolean;
  autoGenerateBookingNo: boolean;
  invoiceHeader: string;
  invoiceFooter: string;
  isSingleBranch: boolean;
  codeGeneration: number;
  isActive: boolean;
  status: string;
}

export interface ICompanyRequest {
  name: string;
  logoUrl?: string;
  businessCurrency?: string;
  currencySymbol?: string;
  description?: string;
  autoInvoicePrint: boolean;
  autoGenerateBookingNo: boolean;
  invoiceHeader?: string;
  invoiceFooter?: string;
  isSingleBranch: boolean;
  codeGeneration: number;
  isActive: boolean;
}

export enum ECodeGeneration {
  Auto = 0,
  DailyCount = 1,
  Company = 2,
  Branch = 3,
}
