import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { ICashBookReport } from '../models/cashbook.interface';

@Injectable({
  providedIn: 'root',
})
export class CashBookService {
  private apiUrl = `${environment.apiUrl}/CashBook`;

  constructor(private http: HttpClient) {}

  getCashBook(reportDate: Date): Observable<ICashBookReport> {
    let params = new HttpParams();
    params = params.append('reportDate', reportDate.toISOString());

    return this.http.get<ICashBookReport>(this.apiUrl, { params });
  }
}
