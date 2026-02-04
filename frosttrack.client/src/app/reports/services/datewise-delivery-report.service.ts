import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IDatewiseDeliveryReportItem } from '../models/datewise-delivery-report.interface';

@Injectable({
  providedIn: 'root',
})
export class DatewiseDeliveryReportService {
  private apiUrl = `${environment.apiUrl}/DatewiseDeliveryReport`;

  constructor(private http: HttpClient) {}

  getDatewiseDeliveryReport(
    fromDate?: Date,
    toDate?: Date,
    customerId?: number,
    productId?: number,
  ): Observable<IDatewiseDeliveryReportItem[]> {
    let params = new HttpParams();

    if (fromDate) {
      params = params.set('fromDate', fromDate.toISOString());
    }

    if (toDate) {
      params = params.set('toDate', toDate.toISOString());
    }

    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }

    if (productId) {
      params = params.set('productId', productId.toString());
    }

    return this.http.get<IDatewiseDeliveryReportItem[]>(this.apiUrl, {
      params,
    });
  }
}
