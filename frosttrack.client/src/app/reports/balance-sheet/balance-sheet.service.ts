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

  getBalanceSheet(
    asOfDate: string,
    branchId?: number
  ): Observable<IBalanceSheetSummary> {
    let params = new HttpParams().set('asOfDate', asOfDate);

    if (branchId) {
      params = params.set('branchId', branchId.toString());
    }

    return this.http.get<IBalanceSheetSummary>(this.apiUrl, { params });
  }
}
