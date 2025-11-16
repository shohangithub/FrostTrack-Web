import { ISupplierResponse } from '../../common/models/supplier.interface';
import { ICustomerResponse } from '../../common/models/customer.interface';
import { IBankResponse } from '../../common/models/bank.interface';

export interface ISupplierPaymentRequest {
  id: number;
  paymentNumber: string;
  paymentDate: Date;
  paymentType: string; // Always "Supplier" for this component
  supplierId: number;
  paymentMethod: string; // "Cash", "Bank", "Check", "Online", "MobileWallet", "Card"
  bankId?: number;
  checkNumber?: string;
  checkDate?: Date;
  // Online payment fields
  onlinePaymentMethod?: string;
  transactionId?: string;
  gatewayReference?: string;
  // Mobile wallet fields
  mobileWalletType?: string;
  walletNumber?: string;
  walletTransactionId?: string;
  // Card payment fields
  cardType?: string;
  cardLastFour?: string;
  cardTransactionId?: string;
  paymentAmount: number;
  notes?: string;
  branchId: number;
}

export interface ISupplierPaymentResponse {
  id: number;
  paymentNumber: string;
  paymentDate: Date;
  paymentType: string;
  supplierId?: number;
  supplier?: ISupplierResponse;
  customerId?: number;
  customer?: ICustomerResponse;
  paymentMethod: string;
  bankId?: number;
  bank?: IBankResponse;
  checkNumber?: string;
  checkDate?: Date;
  // Online payment fields
  onlinePaymentMethod?: string;
  transactionId?: string;
  gatewayReference?: string;
  // Mobile wallet fields
  mobileWalletType?: string;
  walletNumber?: string;
  walletTransactionId?: string;
  // Card payment fields
  cardType?: string;
  cardLastFour?: string;
  cardTransactionId?: string;
  paymentAmount: number;
  notes?: string;
  branchId: number;
}

export interface ISupplierPaymentListResponse {
  id: number;
  paymentNumber: string;
  paymentDate: Date;
  paymentType: string;
  supplierId?: number;
  supplier?: ISupplierResponse;
  customerId?: number;
  customer?: ICustomerResponse;
  paymentMethod: string;
  bankId?: number;
  bank?: IBankResponse;
  checkNumber?: string;
  checkDate?: Date;
  paymentAmount: number;
  notes?: string;
  branchId: number;
  branch: any;
}

export interface IPendingInvoice {
  id: number;
  invoiceNumber: string;
  invoiceDate: Date;
  invoiceAmount: number;
  paidAmount: number;
  remainingAmount: number;
  supplier?: ISupplierResponse;
  customer?: ICustomerResponse;
}
