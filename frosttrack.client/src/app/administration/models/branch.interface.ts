export interface IBranchListResponse {
  id: number;
  name: string;
  branchCode: string;
  businessCurrency?: string;
  currencySymbol?: string;
  phone: string;
  address: string;
  autoInvoicePrint: boolean;
  invoiceHeader?: string;
  invoiceFooter?: string;
  isMainBranch: boolean;
  status: string;
}

export interface IBranchResponse {
  id: number;
  name: string;
  branchCode: string;
  businessCurrency?: string;
  currencySymbol?: string;
  phone: string;
  address: string;
  autoInvoicePrint: boolean;
  invoiceHeader?: string;
  invoiceFooter?: string;
  isMainBranch: boolean;
  isActive: boolean;
  status: string;
}

export interface IBranchRequest {
  name: string;
  branchCode: string;
  businessCurrency?: string;
  currencySymbol?: string;
  phone: string;
  address: string;
  autoInvoicePrint: boolean;
  invoiceHeader?: string;
  invoiceFooter?: string;
  isMainBranch: boolean;
  isActive: boolean;
}
