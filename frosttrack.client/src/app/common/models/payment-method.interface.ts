export interface IPaymentMethodListResponse {
  id: number;
  methodName: string;
  code: string;
  description?: string;
  category: string;
  requiresBankAccount: boolean;
  requiresCheckDetails: boolean;
  requiresOnlineDetails: boolean;
  requiresMobileWalletDetails: boolean;
  requiresCardDetails: boolean;
  isActive: boolean;
  sortOrder: number;
  iconClass?: string;
  branchId?: number;
  status: string;
}

export interface IPaymentMethodResponse {
  id: number;
  methodName: string;
  code: string;
  description?: string;
  category: string;
  requiresBankAccount: boolean;
  requiresCheckDetails: boolean;
  requiresOnlineDetails: boolean;
  requiresMobileWalletDetails: boolean;
  requiresCardDetails: boolean;
  isActive: boolean;
  sortOrder: number;
  iconClass?: string;
  branchId?: number;
  status: string;
}

export interface IPaymentMethodRequest {
  methodName: string;
  code: string;
  description?: string;
  category: string;
  requiresBankAccount: boolean;
  requiresCheckDetails: boolean;
  requiresOnlineDetails: boolean;
  requiresMobileWalletDetails: boolean;
  requiresCardDetails: boolean;
  isActive: boolean;
  sortOrder: number;
  iconClass?: string;
  branchId?: number;
}
