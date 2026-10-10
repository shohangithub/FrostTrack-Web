import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ITransactionGroupListResponse,
  ITransactionGroupRequest,
  ITransactionGroupResponse,
} from '../models/transaction-group.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class TransactionGroupService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/transactiongroup`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ITransactionGroupListResponse>> {
    return this.get<PaginationResult<ITransactionGroupListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Transaction Groups pagination'
    );
  }

  getById(id: string): Observable<ITransactionGroupResponse> {
    return this.get<ITransactionGroupResponse>(
      this.path + '/' + id,
      'Load Transaction Group'
    );
  }

  create(
    payload: ITransactionGroupRequest
  ): Observable<ITransactionGroupResponse> {
    return this.postWithSuccess<ITransactionGroupResponse>(
      this.path,
      payload,
      'Create Transaction Group',
      MessageHub.ADD
    );
  }

  update(
    id: string,
    payload: ITransactionGroupRequest
  ): Observable<ITransactionGroupResponse> {
    return this.putWithSuccess<ITransactionGroupResponse>(
      this.path + '/' + id,
      payload,
      'Update Transaction Group',
      MessageHub.UPDATE
    );
  }

  remove(id: string): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Transaction Group',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/DeleteBatch',
      ids,
      `Delete ${ids.length} Transaction Groups`,
      MessageHub.DELETE_BATCH
    );
  }

  getLookup(): Observable<ILookup<string>[]> {
    return this.get<ILookup<string>[]>(
      this.path + `/lookup`,
      'Load Transaction Groups Lookup'
    );
  }
}
