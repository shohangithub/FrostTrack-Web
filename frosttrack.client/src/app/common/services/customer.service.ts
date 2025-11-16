import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ICustomerListResponse,
  ICustomerRequest,
  ICustomerResponse,
} from '../models/customer.interface';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class CustomerService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/customer`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ICustomerListResponse>> {
    return this.get<PaginationResult<ICustomerListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Customers pagination'
    );
  }

  getList(): Observable<ICustomerListResponse[]> {
    return this.get<ICustomerListResponse[]>(
      this.path + `/get-list`,
      'Load Customer List'
    );
  }

  getById(id: number): Observable<ICustomerResponse> {
    return this.get<ICustomerResponse>(this.path + '/' + id, 'Load Customer');
  }

  create(payload: ICustomerRequest): Observable<ICustomerResponse> {
    return this.postWithSuccess<ICustomerResponse>(
      this.path,
      payload,
      'Create Customer',
      MessageHub.ADD
    );
  }

  update(id: number, payload: ICustomerRequest): Observable<ICustomerResponse> {
    return this.putWithSuccess<ICustomerResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Customer',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Customer',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/deletebatch`,
      ids,
      'Delete Customers',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Generate Customer Code'
    );
  }
}
