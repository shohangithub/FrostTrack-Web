import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { ICustomerDueSummaryResponse } from 'app/booking/models/booking.interface';

@Injectable({
  providedIn: 'root',
})
export class CustomerDueReportService {
  private apiUrl = `${environment.apiUrl}/Booking/customer-due-summary`;

  constructor(private http: HttpClient) {}

  getCustomerDueSummary(
    reportDate?: Date,
    customerId?: number | null,
    status?: string | null,
    dueOnly?: boolean | null,
    searchTerm?: string | null,
  ): Observable<ICustomerDueSummaryResponse[]> {
    let params = new HttpParams();

    if (reportDate) {
      params = params.set('reportDate', reportDate.toISOString());
    }

    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }

    if (status && status !== 'all') {
      params = params.set('status', status);
    }

    if (dueOnly !== null && dueOnly !== undefined) {
      params = params.set('dueOnly', dueOnly.toString());
    }

    if (searchTerm && searchTerm.trim()) {
      params = params.set('searchTerm', searchTerm.trim());
    }

    return this.http.get<ICustomerDueSummaryResponse[]>(this.apiUrl, { params });
  }
}
