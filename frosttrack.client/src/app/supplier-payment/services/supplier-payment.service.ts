import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ISupplierPaymentListResponse,
  ISupplierPaymentRequest,
  ISupplierPaymentResponse,
} from '../models/supplier-payment.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { IPurchaseListResponse } from '../../purchase/models/purchase.interface';
import { ISalesListResponse } from '../../sales/models/sales.interface';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class SupplierPaymentService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/supplierpayment`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ISupplierPaymentListResponse>> {
    return this.get<PaginationResult<ISupplierPaymentListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`)
    );
  }

  getById(id: number): Observable<ISupplierPaymentResponse> {
    return this.get<ISupplierPaymentResponse>(this.path + '/' + id);
  }

  create(
    payload: ISupplierPaymentRequest
  ): Observable<ISupplierPaymentResponse> {
    return this.postWithSuccess<ISupplierPaymentResponse>(
      this.path,
      payload,
      'Create Supplier Payment',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: ISupplierPaymentRequest
  ): Observable<ISupplierPaymentResponse> {
    return this.putWithSuccess<ISupplierPaymentResponse>(
      this.path + '/' + id,
      payload,
      'Update Supplier Payment',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Supplier Payment',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/DeleteBatch',
      ids,
      `Delete ${ids.length} Supplier Payments`,
      MessageHub.DELETE_BATCH
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Supplier Payment Lookup'
    );
  }

  generatePaymentNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-payment-number',
      'Generate Payment Number'
    );
  }

  getSupplierDueBalance(supplierId: number): Observable<number> {
    return this.get<number>(
      this.path + `/supplier-due-balance/${supplierId}`,
      'Get Supplier Due Balance'
    );
  }

  getPendingPurchases(supplierId: number): Observable<IPurchaseListResponse[]> {
    return this.get<IPurchaseListResponse[]>(
      this.path + `/pending-purchases/${supplierId}`,
      'Get Pending Purchases'
    );
  }

  getPendingSales(customerId: number): Observable<ISalesListResponse[]> {
    return this.get<ISalesListResponse[]>(
      this.path + `/pending-sales/${customerId}`,
      'Get Pending Sales'
    );
  }
}
