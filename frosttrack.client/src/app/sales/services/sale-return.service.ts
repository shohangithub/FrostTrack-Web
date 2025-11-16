import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ISaleReturnListResponse,
  ISaleReturnRequest,
  ISaleReturnResponse,
} from '../models/sale-return.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class SaleReturnService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/salereturn`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ISaleReturnListResponse>> {
    return this.get<PaginationResult<ISaleReturnListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Sale Returns pagination'
    );
  }

  getById(id: number): Observable<ISaleReturnResponse> {
    return this.get<ISaleReturnResponse>(
      this.path + '/' + id,
      'Load Sale Return'
    );
  }

  create(payload: ISaleReturnRequest): Observable<ISaleReturnResponse> {
    return this.postWithSuccess<ISaleReturnResponse>(
      this.path,
      payload,
      'Create Sale Return',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: ISaleReturnRequest
  ): Observable<ISaleReturnResponse> {
    return this.putWithSuccess<ISaleReturnResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Sale Return',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Sale Return',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/DeleteBatch',
      ids,
      `Delete ${ids.length} Sale Returns`,
      MessageHub.DELETE_BATCH
    );
  }

  generateReturnNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-return-number',
      'Generate Return Number'
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Sale Returns Lookup'
    );
  }

  getSalesBySalesId(salesId: number): Observable<any> {
    return this.get<any>(
      this.path + `/sales/${salesId}`,
      'Load Sale by Sales ID'
    );
  }
}
