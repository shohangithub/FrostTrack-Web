import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ISalesListResponse,
  ISalesRequest,
  ISalesResponse,
} from '../models/sales.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class SalesService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/sales`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ISalesListResponse>> {
    return this.get<PaginationResult<ISalesListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Sales pagination'
    );
  }

  getById(id: number): Observable<ISalesResponse> {
    return this.get<ISalesResponse>(this.path + '/' + id, 'Load Sales');
  }

  create(payload: ISalesRequest): Observable<ISalesResponse> {
    return this.postWithSuccess<ISalesResponse>(
      this.path,
      payload,
      'Create Sales',
      MessageHub.ADD
    );
  }

  update(id: number, payload: ISalesRequest): Observable<ISalesResponse> {
    return this.putWithSuccess<ISalesResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Sales',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Sales',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/DeleteBatch',
      ids,
      `Delete ${ids.length} Sales`,
      MessageHub.DELETE_BATCH
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Sales Lookup'
    );
  }

  generateInvoiceNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-invoice-number',
      'Generate Invoice Number'
    );
  }
}
