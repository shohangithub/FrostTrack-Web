import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { PaginationQuery } from '@core/models/pagination-query';
import { PaginationResult } from '@core/models/pagination-result';
import {
  ITransactionListResponse,
  ITransactionDetailResponse,
  ITransactionRequest,
} from '../models/transaction.interface';

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private apiUrl = `${environment.apiUrl}/transaction`;

  constructor(private http: HttpClient) {}

  getWithPagination(
    query: PaginationQuery
  ): Observable<PaginationResult<ITransactionListResponse>> {
    let params = new HttpParams()
      .set('pageIndex', query.pageIndex.toString())
      .set('pageSize', query.pageSize.toString())
      .set('orderBy', query.orderBy || '')
      .set('isAscending', (query.isAscending ?? true).toString());

    if (query.openText) {
      params = params.set('openText', query.openText);
    }

    return this.http.get<PaginationResult<ITransactionListResponse>>(
      this.apiUrl,
      { params }
    );
  }

  getById(id: string): Observable<ITransactionDetailResponse> {
    return this.http.get<ITransactionDetailResponse>(`${this.apiUrl}/${id}`);
  }

  create(request: ITransactionRequest): Observable<ITransactionDetailResponse> {
    return this.http.post<ITransactionDetailResponse>(this.apiUrl, request);
  }

  update(
    id: string,
    request: ITransactionRequest
  ): Observable<ITransactionDetailResponse> {
    return this.http.put<ITransactionDetailResponse>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  remove(id: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/${id}`);
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/batch-delete`, { ids });
  }

  softDelete(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/soft-delete`, {});
  }

  restore(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {});
  }

  archive(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/archive`, {});
  }

  unarchive(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/unarchive`, {});
  }

  getByEntityReference(
    entityName: string,
    entityId: string
  ): Observable<ITransactionListResponse[]> {
    return this.http.get<ITransactionListResponse[]>(
      `${this.apiUrl}/by-entity/${entityName}/${entityId}`
    );
  }

  getTransactionCode(): Observable<{ code: string }> {
    return this.http.get<{ code: string }>(`${this.apiUrl}/generate-code`);
  }
}
