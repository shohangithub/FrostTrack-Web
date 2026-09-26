import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { PaginationQuery } from '@core/models/pagination-query';
import { PaginationResult } from '@core/models/pagination-result';
import {
  ITransactionListResponse,
  ITransactionDetailResponse,
  ITransactionRequest,
} from '../models/transaction.interface';
import { CodeResponse } from '@core/models/code-response';
import { ILookup } from '@core/models/lookup';
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';
import { getApiEndpoint } from 'app/utils/api-builder';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class TransactionService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService,
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/transaction`;

  getWithPagination(
    pagination: PaginationQuery,
  ): Observable<PaginationResult<ITransactionListResponse>> {
    return this.get<PaginationResult<ITransactionListResponse>>(
      getApiEndpoint(pagination, this.path + `/pagination`),
      'Load Transactions',
    );
  }

  getById(id: string): Observable<ITransactionDetailResponse> {
    return this.get<ITransactionDetailResponse>(
      this.path + '/' + id,
      'Load Transaction',
    );
  }

  create(payload: ITransactionRequest): Observable<ITransactionDetailResponse> {
    return this.postWithSuccess<ITransactionDetailResponse>(
      this.path,
      payload,
      'Create Transaction',
      MessageHub.ADD,
    );
  }

  update(
    id: string,
    payload: ITransactionRequest,
  ): Observable<ITransactionDetailResponse> {
    return this.putWithSuccess<ITransactionDetailResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Transaction',
      MessageHub.UPDATE,
    );
  }

  remove(id: string): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Transaction',
      MessageHub.DELETE_ONE,
    );
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Transactions',
      `${ids.length} ${MessageHub.DELETE_BATCH}`,
    );
  }

  softDelete(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/soft-delete`,
      {},
      'Soft Delete Transaction',
    );
  }

  restore(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/restore`,
      {},
      'Restore Transaction',
    );
  }

  archive(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/archive`,
      {},
      'Archive Transaction',
    );
  }

  unarchive(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/unarchive`,
      {},
      'Unarchive Transaction',
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Transaction Code Generation',
    );
  }

  getLookup(): Observable<ILookup<string>[]> {
    return this.get<ILookup<string>[]>(
      this.path + '/lookup',
      'Load Transaction Lookup',
    );
  }

  getLookupByUsageFor(usageFor: string): Observable<ILookup<string>[]> {
    return this.get<ILookup<string>[]>(
      `${this.path}/lookup-by-usage-for?usageFor=${usageFor}`,
      'Load Transaction Lookup by UsageFor',
    );
  }

  getByTransactionCode(
    transactionCode: string,
  ): Observable<ITransactionDetailResponse> {
    return this.get<ITransactionDetailResponse>(
      `${this.path}/by-code/${transactionCode}`,
      'Load Transaction by Code',
    );
  }

  getTransactionReport(
    startDate: Date | string,
    endDate?: Date | string,
    reportDate?: Date | string,
  ): Observable<ITransactionListResponse[]> {
    const toDate = endDate || startDate;
    const formatParam = (d: Date | string): string => {
      if (typeof d === 'string') {
        const trimmed = d.trim();
        if (/^\d{4}-\d{2}-\d{2}/.test(trimmed)) {
          return trimmed.substring(0, 10);
        }
        return trimmed;
      }
      const year = d.getFullYear();
      const month = (d.getMonth() + 1).toString().padStart(2, '0');
      const day = d.getDate().toString().padStart(2, '0');
      return `${year}-${month}-${day}`;
    };

    const params = new URLSearchParams({
      startDate: formatParam(startDate),
      endDate: formatParam(toDate),
      reportDate: formatParam(reportDate || startDate),
    });
    return this.get<ITransactionListResponse[]>(
      `${this.path}?${params.toString()}`,
      'Load Transaction Report',
    );
  }
}
