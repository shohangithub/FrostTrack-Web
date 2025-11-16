import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ISupplierListResponse,
  ISupplierRequest,
  ISupplierResponse,
} from '../models/supplier.interface';
import { CodeResponse } from '../../core/models/code-response';
import { ILookup } from '@core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class SupplierService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/supplier`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ISupplierListResponse>> {
    return this.get<PaginationResult<ISupplierListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Suppliers pagination'
    );
  }

  getList(): Observable<ISupplierListResponse[]> {
    return this.get<ISupplierListResponse[]>(
      this.path + `/get-list`,
      'Load Supplier List'
    );
  }

  getById(id: number): Observable<ISupplierResponse> {
    return this.get<ISupplierResponse>(this.path + '/' + id, 'Load Supplier');
  }

  create(payload: ISupplierRequest): Observable<ISupplierResponse> {
    return this.postWithSuccess<ISupplierResponse>(
      this.path,
      payload,
      'Create Supplier',
      MessageHub.ADD
    );
  }

  update(id: number, payload: ISupplierRequest): Observable<ISupplierResponse> {
    return this.putWithSuccess<ISupplierResponse>(
      this.path + '/' + id,
      payload,
      'Update Supplier',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Supplier',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/deletebatch',
      ids,
      `Delete ${ids.length} Suppliers`,
      MessageHub.DELETE_BATCH
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Generate Supplier Code'
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Supplier Lookup'
    );
  }
}
