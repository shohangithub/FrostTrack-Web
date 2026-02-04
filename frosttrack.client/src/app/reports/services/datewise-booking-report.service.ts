import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IDatewiseBookingReportItem } from '../models/datewise-booking-report.interface';

@Injectable({
  providedIn: 'root',
})
export class DatewiseBookingReportService {
  private apiUrl = `${environment.apiUrl}/DatewiseBookingReport`;

  constructor(private http: HttpClient) {}

  getDatewiseBookingReport(
    fromDate?: Date,
    toDate?: Date,
    customerId?: number,
    productId?: number,
  ): Observable<IDatewiseBookingReportItem[]> {
    let params = new HttpParams();

    if (fromDate) {
      params = params.set('fromDate', fromDate.toLocaleDateString());
    }

    if (toDate) {
      params = params.set('toDate', toDate.toLocaleDateString());
    }

    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }

    if (productId) {
      params = params.set('productId', productId.toString());
    }

    return this.http.get<IDatewiseBookingReportItem[]>(this.apiUrl, { params });
  }
}
