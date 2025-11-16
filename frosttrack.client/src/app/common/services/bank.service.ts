import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IBankListResponse,
  IBankRequest,
  IBankResponse,
} from '../models/bank.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import { CodeResponse } from '@core/models/code-response';

@Injectable({ providedIn: 'root' })
export class BankService extends BaseService {
  path: string = `${environment.apiUrl}/bank`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IBankListResponse>> {
    return this.get<PaginationResult<IBankListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Banks'
    );
  }

  getById(id: number): Observable<IBankResponse> {
    return this.get<IBankResponse>(this.path + '/' + id, 'Load Bank');
  }

  create(payload: IBankRequest): Observable<IBankResponse> {
    return this.postWithSuccess<IBankResponse>(
      this.path,
      payload,
      'Create Bank',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IBankRequest): Observable<IBankResponse> {
    return this.putWithSuccess<IBankResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Bank',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Bank',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Banks',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Bank Lookup'
    );
  }

  generateCode(isGlobal: boolean = false): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      `${this.path}/generate-code?isGlobal=${isGlobal}`,
      'Bank Code Generation'
    );
  }

  getCurrentBalance(bankId: number): Observable<number> {
    return this.get<number>(
      `${this.path}/${bankId}/balance`,
      'Load Bank Balance'
    );
  }
}
