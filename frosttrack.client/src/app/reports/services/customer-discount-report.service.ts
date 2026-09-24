import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { ICustomerDiscountItem } from 'app/bill-collection/services/bill-collection.service';

@Injectable({
  providedIn: 'root',
})
export class CustomerDiscountReportService {
  private apiUrl = `${environment.apiUrl}/BillCollection/discount-report`;

  constructor(private http: HttpClient) {}

  getCustomerDiscountReport(
    startDate?: Date | string | null,
    endDate?: Date | string | null,
    customerId?: number | null,
    searchTerm?: string | null,
  ): Observable<ICustomerDiscountItem[]> {
    let params = new HttpParams();

    if (startDate) {
      const s = typeof startDate === 'string' ? startDate : startDate.toISOString();
      params = params.set('startDate', s);
    }

    if (endDate) {
      const e = typeof endDate === 'string' ? endDate : endDate.toISOString();
      params = params.set('endDate', e);
    }

    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }

    if (searchTerm && searchTerm.trim()) {
      params = params.set('searchTerm', searchTerm.trim());
    }

    return this.http.get<ICustomerDiscountItem[]>(this.apiUrl, { params });
  }
}
