import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';
import {
  IBookingWithDueResponse,
  IBookingLookupWithDue,
} from '../models/bill-collection.interface';
import {
  IBillCollectionRequest,
  ITransactionDetailResponse,
} from 'app/transaction/models/transaction.interface';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class BillCollectionService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }

  path: string = `${environment.apiUrl}/BillCollection`;

  // Get bookings with due amounts for lookup
  getBookingsWithDue(): Observable<IBookingLookupWithDue[]> {
    return this.get<IBookingLookupWithDue[]>(
      `${this.path}/bookings-with-due`,
      'Load Bookings with Due'
    );
  }

  // Get booking details including financial information
  getBookingForBillCollection(
    bookingId: string
  ): Observable<IBookingWithDueResponse> {
    return this.get<IBookingWithDueResponse>(
      `${this.path}/booking/${bookingId}`,
      'Load Booking Details'
    );
  }

  // Create bill collection
  createBillCollection(
    payload: IBillCollectionRequest
  ): Observable<ITransactionDetailResponse> {
    return this.postWithSuccess<ITransactionDetailResponse>(
      this.path,
      payload,
      'Create Bill Collection',
      MessageHub.ADD
    );
  }

  // Update bill collection
  updateBillCollection(
    id: string,
    payload: IBillCollectionRequest
  ): Observable<ITransactionDetailResponse> {
    return this.putWithSuccess<ITransactionDetailResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Bill Collection',
      MessageHub.UPDATE
    );
  }
}
