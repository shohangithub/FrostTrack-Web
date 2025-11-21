export enum TransactionType {
  BILL_COLLECTION = 'BILL_COLLECTION',
  OFFICE_EXPENSE = 'OFFICE_EXPENSE',
  BILL_PAYMENT = 'BILL_PAYMENT',
  ADVANCE_PAYMENT = 'ADVANCE_PAYMENT',
  OTHER = 'OTHER',
}

export enum TransactionFlow {
  IN = 'IN',
  OUT = 'OUT',
}

export enum PaymentMethod {
  CASH = 'CASH',
  BANK_TRANSFER = 'BANK_TRANSFER',
  CHEQUE = 'CHEQUE',
  MOBILE_BANKING = 'MOBILE_BANKING',
  CARD = 'CARD',
  OTHER = 'OTHER',
}

export enum ExpenseCategory {
  RENT = 'RENT',
  UTILITIES = 'UTILITIES',
  SALARIES = 'SALARIES',
  TRANSPORTATION = 'TRANSPORTATION',
  OFFICE_SUPPLIES = 'OFFICE_SUPPLIES',
  MAINTENANCE = 'MAINTENANCE',
  FUEL = 'FUEL',
  INSURANCE = 'INSURANCE',
  TAX = 'TAX',
  COMMISSION = 'COMMISSION',
  ADVERTISING = 'ADVERTISING',
  TELECOMMUNICATION = 'TELECOMMUNICATION',
  EQUIPMENT = 'EQUIPMENT',
  PROFESSIONAL_FEES = 'PROFESSIONAL_FEES',
  MEALS_ENTERTAINMENT = 'MEALS_ENTERTAINMENT',
  TRAVEL = 'TRAVEL',
  MISCELLANEOUS = 'MISCELLANEOUS',
  OTHER = 'OTHER',
}

export interface ITransactionListResponse {
  id: string;
  transactionCode: string;
  transactionDate: Date | string;
  transactionType: string;
  transactionFlow: string;
  branchId: number;
  branchName: string;
  customerId?: number | null;
  customerName?: string | null;
  netAmount: number;
  paymentMethod: string;
  category?: string | null;
  description: string;
  vendorName?: string | null;
}

export interface ITransactionRequest {
  id?: string;
  transactionCode: string;
  transactionType: TransactionType;
  transactionFlow: TransactionFlow;
  transactionDate: Date | string;
  branchId: number;
  amount: number;
  note?: string;
  // Optional fields with defaults
  entityName?: string;
  entityId?: string;
  customerId?: number | null;
  bookingId?: string | null;
  discountAmount?: number;
  adjustmentValue?: number;
  paymentMethod?: PaymentMethod | string;
  paymentReference?: string;
  category?: ExpenseCategory | string;
  subCategory?: string;
  description?: string;
  vendorName?: string;
  vendorContact?: string;
  billingPeriodStart?: Date | null;
  billingPeriodEnd?: Date | null;
  attachmentPath?: string;
}

export interface ITransactionDetailResponse extends ITransactionListResponse {
  amount: number;
  discountAmount: number;
  adjustmentValue: number;
  note?: string | null;
  entityName?: string;
  entityId?: string;
  bookingId?: string | null;
  paymentReference?: string | null;
  subCategory?: string | null;
  vendorContact?: string | null;
  billingPeriodStart?: Date | null;
  billingPeriodEnd?: Date | null;
  attachmentPath?: string | null;
  updatedAt?: Date;
  updatedBy?: string;
  deletedBy?: string;
  archivedBy?: string;
}
