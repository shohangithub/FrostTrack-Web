import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IProductReceiveListResponse,
  IProductReceiveRequest,
  IProductReceiveResponse,
} from '../models/product-receive.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';

@Injectable({ providedIn: 'root' })
export class ProductReceiveService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/productreceive`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IProductReceiveListResponse>> {
    return this.get<PaginationResult<IProductReceiveListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`)
    );
  }

  getById(id: number): Observable<IProductReceiveResponse> {
    return this.get<IProductReceiveResponse>(this.path + '/' + id);
  }

  create(payload: IProductReceiveRequest): Observable<IProductReceiveResponse> {
    return this.post<IProductReceiveResponse>(this.path, payload);
  }

  update(
    id: number,
    payload: IProductReceiveRequest
  ): Observable<IProductReceiveResponse> {
    return this.put<IProductReceiveResponse>(this.path + '/' + id, payload);
  }

  remove(id: number): Observable<boolean> {
    return this.delete<boolean>(this.path + '/' + id);
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.post<boolean>(this.path + '/DeleteBatch', ids);
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(this.path + `/lookup`);
  }

  generateReceiveNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(this.path + '/generate-receive-number');
  }
}
