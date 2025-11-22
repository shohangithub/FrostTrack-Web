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
}
