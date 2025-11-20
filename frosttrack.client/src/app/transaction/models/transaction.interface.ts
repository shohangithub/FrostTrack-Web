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
  transactionType: TransactionType;
  transactionFlow: TransactionFlow;
  transactionDate: Date;
  amount: number;
  discountAmount: number;
  adjustmentValue: number;
  netAmount: number;
  paymentMethod: PaymentMethod;
  category?: ExpenseCategory;
  entityName?: string;
  entityId?: string;
  customerId?: string;
  customerName?: string;
  supplierId?: string;
  supplierName?: string;
  bankId?: string;
  bankName?: string;
  bankTransactionId?: string;
  description?: string;
  notes?: string;
  isDeleted: boolean;
  deletedAt?: Date;
  isArchived: boolean;
  archivedAt?: Date;
  createdAt: Date;
  createdBy?: string;
}

export interface ITransactionRequest {
  transactionCode: string;
  transactionType: TransactionType;
  transactionFlow: TransactionFlow;
  transactionDate: Date;
  amount: number;
  discountAmount: number;
  adjustmentValue: number;
  paymentMethod: PaymentMethod;
  category?: ExpenseCategory;
  entityName?: string;
  entityId?: string;
  customerId?: string;
  supplierId?: string;
  bankId?: string;
  bankTransactionId?: string;
  description?: string;
  notes?: string;
}

export interface ITransactionDetailResponse extends ITransactionListResponse {
  updatedAt?: Date;
  updatedBy?: string;
  deletedBy?: string;
  archivedBy?: string;
}
