import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IDailyStockBookItem } from '../models/daily-stock-book.interface';

@Injectable({
  providedIn: 'root',
})
export class DailyStockBookService {
  private apiUrl = `${environment.apiUrl}/DailyStockBook`;

  constructor(private http: HttpClient) {}

  getDailyStockBook(
    reportDate?: Date,
    customerId?: number,
    productId?: number
  ): Observable<IDailyStockBookItem[]> {
    let params = new HttpParams();

    if (reportDate) {
      params = params.set('reportDate', reportDate.toISOString());
    }

    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }

    if (productId) {
      params = params.set('productId', productId.toString());
    }

    return this.http.get<IDailyStockBookItem[]>(this.apiUrl, { params });
  }
}
