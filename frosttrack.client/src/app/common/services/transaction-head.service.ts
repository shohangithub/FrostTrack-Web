import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ITransactionHeadListResponse,
  ITransactionHeadRequest,
  ITransactionHeadResponse,
  ITransactionHeadLookup,
} from '../models/transaction-head.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class TransactionHeadService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/transactionhead`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ITransactionHeadListResponse>> {
    return this.get<PaginationResult<ITransactionHeadListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Transaction Heads pagination'
    );
  }

  getById(id: number): Observable<ITransactionHeadResponse> {
    return this.get<ITransactionHeadResponse>(
      this.path + '/' + id,
      'Load Transaction Head'
    );
  }

  create(
    payload: ITransactionHeadRequest
  ): Observable<ITransactionHeadResponse> {
    return this.postWithSuccess<ITransactionHeadResponse>(
      this.path,
      payload,
      'Create Transaction Head',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: ITransactionHeadRequest
  ): Observable<ITransactionHeadResponse> {
    return this.putWithSuccess<ITransactionHeadResponse>(
      this.path + '/' + id,
      payload,
      'Update Transaction Head',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Transaction Head',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/DeleteBatch',
      ids,
      `Delete ${ids.length} Transaction Heads`,
      MessageHub.DELETE_BATCH
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Transaction Heads Lookup'
    );
  }

  getTransactionLookup(): Observable<ITransactionHeadLookup[]> {
    return this.get<ITransactionHeadLookup[]>(
      this.path + `/TransactionLookup`,
      'Load Transaction Lookup'
    );
  }
}
