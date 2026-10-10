import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IBankBookReport, IBankBookRangeReport } from '../models/bankbook.interface';

@Injectable({
  providedIn: 'root',
})
export class BankBookService {
  private apiUrl = `${environment.apiUrl}/BankBook`;

  constructor(private http: HttpClient) {}

  getBankBook(reportDate: Date, bankId?: number | null): Observable<IBankBookReport> {
    let params = new HttpParams();
    params = params.append('reportDate', reportDate.toISOString());
    if (bankId) {
      params = params.append('bankId', bankId.toString());
    }

    return this.http.get<IBankBookReport>(this.apiUrl, { params });
  }

  getBankBookRange(fromDate: Date, toDate: Date, bankId?: number | null): Observable<IBankBookRangeReport> {
    let params = new HttpParams();
    params = params.append('fromDate', fromDate.toISOString());
    params = params.append('toDate', toDate.toISOString());
    if (bankId) {
      params = params.append('bankId', bankId.toString());
    }

    return this.http.get<IBankBookRangeReport>(`${this.apiUrl}/range`, { params });
  }
}
