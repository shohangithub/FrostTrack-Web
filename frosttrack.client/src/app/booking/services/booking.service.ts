import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IBookingListResponse,
  IBookingRequest,
  IBookingResponse,
} from '../models/booking.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';

@Injectable({ providedIn: 'root' })
export class BookingService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/booking`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IBookingListResponse>> {
    return this.get<PaginationResult<IBookingListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`)
    );
  }

  getById(id: string): Observable<IBookingResponse> {
    return this.get<IBookingResponse>(this.path + '/' + id);
  }

  create(payload: IBookingRequest): Observable<IBookingResponse> {
    return this.post<IBookingResponse>(this.path, payload);
  }

  update(id: string, payload: IBookingRequest): Observable<IBookingResponse> {
    return this.put<IBookingResponse>(this.path + '/' + id, payload);
  }

  remove(id: string): Observable<boolean> {
    return this.delete<boolean>(this.path + '/' + id);
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.post<boolean>(this.path + '/DeleteBatch', ids);
  }

  getLookup(): Observable<ILookup<string>[]> {
    return this.get<ILookup<string>[]>(this.path + `/lookup`);
  }

  generateBookingNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(this.path + '/generate-booking-number');
  }
}
