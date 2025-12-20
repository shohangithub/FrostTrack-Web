import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IBalanceSheetSummary } from './balance-sheet.interfaces';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class BalanceSheetService {
  private apiUrl = `${environment.apiUrl}/BalanceSheet`;

  constructor(private http: HttpClient) {}

  getBalanceSheet(reportDate: Date): Observable<IBalanceSheetSummary> {
    const params = new HttpParams().set('reportDate', reportDate.toISOString());

    return this.http.get<IBalanceSheetSummary>(this.apiUrl, { params });
  }
}
