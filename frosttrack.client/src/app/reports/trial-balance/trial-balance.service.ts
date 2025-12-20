import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ITrialBalanceSummary } from './trial-balance.interfaces';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class TrialBalanceService {
  private apiUrl = `${environment.apiUrl}/TrialBalance`;

  constructor(private http: HttpClient) {}

  getTrialBalance(reportDate: Date): Observable<ITrialBalanceSummary> {
    const params = new HttpParams().set('reportDate', reportDate.toISOString());

    return this.http.get<ITrialBalanceSummary>(this.apiUrl, { params });
  }
}
