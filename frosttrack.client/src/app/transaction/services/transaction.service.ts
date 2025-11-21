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
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';
import { getApiEndpoint } from 'app/utils/api-builder';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class TransactionService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/transaction`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ITransactionListResponse>> {
    return this.get<PaginationResult<ITransactionListResponse>>(
      getApiEndpoint(pagination, this.path + `/pagination`),
      'Load Transactions'
    );
  }

  getById(id: string): Observable<ITransactionDetailResponse> {
    return this.get<ITransactionDetailResponse>(
      this.path + '/' + id,
      'Load Transaction'
    );
  }

  create(payload: ITransactionRequest): Observable<ITransactionDetailResponse> {
    return this.postWithSuccess<ITransactionDetailResponse>(
      this.path,
      payload,
      'Create Transaction',
      MessageHub.ADD
    );
  }

  update(
    id: string,
    payload: ITransactionRequest
  ): Observable<ITransactionDetailResponse> {
    return this.putWithSuccess<ITransactionDetailResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Transaction',
      MessageHub.UPDATE
    );
  }

  remove(id: string): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Transaction',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Transactions',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  softDelete(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/soft-delete`,
      {},
      'Soft Delete Transaction'
    );
  }

  restore(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/restore`,
      {},
      'Restore Transaction'
    );
  }

  archive(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/archive`,
      {},
      'Archive Transaction'
    );
  }

  unarchive(id: string): Observable<void> {
    return this.post<void>(
      `${this.path}/${id}/unarchive`,
      {},
      'Unarchive Transaction'
    );
  }

  getByEntityReference(
    entityName: string,
    entityId: string
  ): Observable<ITransactionListResponse[]> {
    return this.get<ITransactionListResponse[]>(
      `${this.path}/by-entity/${entityName}/${entityId}`,
      'Load Entity Transactions'
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Transaction Code Generation'
    );
  }
}
