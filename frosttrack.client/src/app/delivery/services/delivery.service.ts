import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { PaginationQuery } from '@core/models/pagination-query';
import { PaginationResult } from '@core/models/pagination-result';
import {
  IDeliveryRequest,
  IDeliveryResponse,
  IBookingForDeliveryResponse,
} from '../models/delivery.interface';

@Injectable({
  providedIn: 'root',
})
export class DeliveryService {
  private apiUrl = `${environment.apiUrl}/delivery`;

  constructor(private http: HttpClient) {}

  getWithPagination(
    query: PaginationQuery
  ): Observable<PaginationResult<IDeliveryResponse>> {
    let params = new HttpParams()
      .set('pageIndex', query.pageIndex.toString())
      .set('pageSize', query.pageSize.toString())
      .set('orderBy', query.orderBy || '')
      .set('isAscending', (query.isAscending ?? true).toString());

    if (query.openText) {
      params = params.set('openText', query.openText);
    }

    return this.http.get<PaginationResult<IDeliveryResponse>>(
      `${this.apiUrl}/get-with-pagination`,
      { params }
    );
  }

  getById(id: string): Observable<IDeliveryResponse> {
    return this.http.get<IDeliveryResponse>(`${this.apiUrl}/${id}`);
  }

  create(request: IDeliveryRequest): Observable<IDeliveryResponse> {
    return this.http.post<IDeliveryResponse>(this.apiUrl, request);
  }

  update(id: string, request: IDeliveryRequest): Observable<IDeliveryResponse> {
    return this.http.put<IDeliveryResponse>(`${this.apiUrl}/${id}`, request);
  }

  remove(id: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/${id}`);
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/DeleteBatch`, ids);
  }

  getDeliveryNumber(): Observable<{ code: string }> {
    return this.http.get<{ code: string }>(
      `${this.apiUrl}/generate-delivery-number`
    );
  }

  getBookingForDelivery(
    bookingNumber: string
  ): Observable<IBookingForDeliveryResponse> {
    return this.http.get<IBookingForDeliveryResponse>(
      `${this.apiUrl}/booking/${bookingNumber}`
    );
  }

  getBookingLookup(): Observable<{ value: string; text: string }[]> {
    return this.http.get<{ value: string; text: string }[]>(
      `${this.apiUrl}/booking-lookup`
    );
  }

  getBookingPreviousPayments(bookingId: string): Observable<number> {
    return this.http.get<number>(
      `${this.apiUrl}/booking-previous-payments/${bookingId}`
    );
  }
}
