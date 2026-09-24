import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';
import { ITransactionDetailResponse } from 'app/transaction/models/transaction.interface';
import { MessageHub } from '@config/message-hub';

export interface IRecentCustomerPayment {
  id: string;
  transactionCode: string;
  transactionDate: string;
  amount: number;
  paymentMethod: string;
  paymentReference?: string;
  note?: string;
  bankName?: string;
}

export interface ICustomerBalanceSummary {
  customerId: number;
  customerName: string;
  customerMobile: string;
  openingBalance: number;
  totalBookingCharges: number;
  totalDeliveryCharges: number;
  totalRecurringCharges: number;
  totalAccrued: number;
  totalPaid: number;
  totalDiscounts: number;
  netDue: number;
  activeBookingsCount: number;
  recentPayments: IRecentCustomerPayment[];
}

export interface ICustomerPaymentRequest {
  transactionCode: string;
  transactionDate: Date | string;
  branchId: number;
  customerId: number;
  amount: number;
  paymentMethod: string;
  paymentReference?: string;
  note?: string;
  bankId?: number | null;
  bookingId?: string | null;
}

export interface IDeliveryBillCollectionRequest {
  transactionCode: string;
  transactionDate: Date | string;
  branchId: number;
  deliveryIds: string[];
  amount: number;
  paymentMethod: string;
  paymentReference?: string;
  bankId?: number | null;
  note?: string;
  customerId?: number | null;
  bookingId?: string | null;
  advanceAmount?: number;
}

export interface ICustomerPaymentReportItem {
  id: string;
  transactionCode: string;
  transactionDate: string;
  customerId: number;
  customerName: string;
  customerMobile?: string;
  customerAddress?: string;
  amount: number;
  paymentMethod: string;
  bankName?: string;
  paymentReference?: string;
  note?: string;
  createdTime: string;
}

export interface ICustomerDiscountRequest {
  transactionCode: string;
  transactionDate: Date | string;
  branchId: number;
  customerId: number;
  totalDue?: number;
  discountAmount: number;
  currentDue?: number;
  discountReason: string;
  note?: string;
  bookingId?: string | null;
}

export interface ICustomerDiscountItem {
  id: string;
  transactionCode: string;
  transactionDate: string;
  customerId: number;
  customerName: string;
  customerMobile?: string;
  customerAddress?: string;
  totalDue: number;
  discountAmount: number;
  currentDue: number;
  discountReason: string;
  note?: string;
  bookingId?: string | null;
  createdTime?: string;
}

export interface ICustomerPaymentReportFilter {
  startDate?: string;
  endDate?: string;
  customerId?: number;
  paymentMethod?: string;
}

@Injectable({ providedIn: 'root' })
export class BillCollectionService extends BaseService {
  path: string = `${environment.apiUrl}/BillCollection`;

  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }

  // Get live customer balance breakdown & recent payments
  getCustomerBalance(customerId: number): Observable<ICustomerBalanceSummary> {
    return this.get<ICustomerBalanceSummary>(
      `${this.path}/customer-balance/${customerId}`,
      'Get Customer Balance'
    );
  }

  // Create independent customer payment
  createCustomerPayment(
    payload: ICustomerPaymentRequest
  ): Observable<ITransactionDetailResponse> {
    return this.postWithSuccess<ITransactionDetailResponse>(
      `${this.path}/customer-payment`,
      payload,
      'Customer Payment',
      MessageHub.ADD
    );
  }

  // Create customer discount / adjustment voucher
  createCustomerDiscount(
    payload: ICustomerDiscountRequest
  ): Observable<ITransactionDetailResponse> {
    return this.postWithSuccess<ITransactionDetailResponse>(
      `${this.path}/discount`,
      payload,
      'Customer Discount / Adjustment',
      MessageHub.ADD
    );
  }

  // Get customer discount history
  getCustomerDiscountHistory(
    customerId: number
  ): Observable<ICustomerDiscountItem[]> {
    return this.get<ICustomerDiscountItem[]>(
      `${this.path}/customer/${customerId}/discount-history`,
      'Get Customer Discount History'
    );
  }

  // Legacy delivery-based bill collection fallback
  createDeliveryBillCollection(
    payload: IDeliveryBillCollectionRequest
  ): Observable<ITransactionDetailResponse> {
    return this.postWithSuccess<ITransactionDetailResponse>(
      `${this.path}/delivery-based`,
      payload,
      'Delivery Bill Collection',
      MessageHub.ADD
    );
  }

  // Customer Payment Report
  getCustomerPaymentReport(
    filters: ICustomerPaymentReportFilter
  ): Observable<ICustomerPaymentReportItem[]> {
    const params = new URLSearchParams();
    if (filters.startDate) params.set('startDate', filters.startDate);
    if (filters.endDate) params.set('endDate', filters.endDate);
    if (filters.customerId) params.set('customerId', filters.customerId.toString());
    if (filters.paymentMethod) params.set('paymentMethod', filters.paymentMethod);

    return this.get<ICustomerPaymentReportItem[]>(
      `${this.path}/report?${params.toString()}`,
      'Get Customer Payment Report'
    );
  }

  // Customer Discount Report
  getCustomerDiscountReport(
    startDate?: string,
    endDate?: string,
    customerId?: number | null,
    searchTerm?: string | null
  ): Observable<ICustomerDiscountItem[]> {
    const params = new URLSearchParams();
    if (startDate) params.set('startDate', startDate);
    if (endDate) params.set('endDate', endDate);
    if (customerId) params.set('customerId', customerId.toString());
    if (searchTerm && searchTerm.trim()) params.set('searchTerm', searchTerm.trim());

    return this.get<ICustomerDiscountItem[]>(
      `${this.path}/discount-report?${params.toString()}`,
      'Get Customer Discount Report'
    );
  }
}

