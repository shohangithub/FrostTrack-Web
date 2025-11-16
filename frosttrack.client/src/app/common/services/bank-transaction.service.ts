import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IBankTransactionListResponse,
  IBankTransactionRequest,
  IBankTransactionResponse,
} from '../models/bank-transaction.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import { CodeResponse } from '@core/models/code-response';

@Injectable({ providedIn: 'root' })
@Injectable({ providedIn: 'root' })
export class BankTransactionService extends BaseService {
  path: string = `${environment.apiUrl}/banktransaction`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IBankTransactionListResponse>> {
    return this.get<PaginationResult<IBankTransactionListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Bank Transactions'
    );
  }

  getById(id: number): Observable<IBankTransactionResponse> {
    return this.get<IBankTransactionResponse>(
      this.path + '/' + id,
      'Load Bank Transaction'
    );
  }

  create(
    payload: IBankTransactionRequest
  ): Observable<IBankTransactionResponse> {
    return this.postWithSuccess<IBankTransactionResponse>(
      this.path,
      payload,
      'Create Bank Transaction',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: IBankTransactionRequest
  ): Observable<IBankTransactionResponse> {
    return this.putWithSuccess<IBankTransactionResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Bank Transaction',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Bank Transaction',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Bank Transactions',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Bank Transaction Lookup'
    );
  }

  generateCode(isGlobal: boolean = false): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      `${this.path}/generate-code?isGlobal=${isGlobal}`,
      'Transaction Code Generation'
    );
  }
}
