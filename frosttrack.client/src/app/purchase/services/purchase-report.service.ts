import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IPurchaseListResponse,
  IPurchaseRequest,
  IPurchaseResponse,
} from '../models/purchase.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';

@Injectable({ providedIn: 'root' })
export class PurchaseReportService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/purchasereport`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IPurchaseListResponse>> {
    return this.get<PaginationResult<IPurchaseListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-`)
    );
  }

  getInvoiceById(id: number): Observable<IPurchaseResponse> {
    return this.get<IPurchaseResponse>(this.path + '/get-invoice/' + id);
  }

  create(payload: IPurchaseRequest): Observable<IPurchaseResponse> {
    return this.post<IPurchaseResponse>(this.path, payload);
  }

  update(id: number, payload: IPurchaseRequest): Observable<IPurchaseResponse> {
    return this.put<IPurchaseResponse>(this.path + '/' + id, payload);
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

  generateInvoiceNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(this.path + '/generate-invoice-number');
  }
}
