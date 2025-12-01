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

  getTrialBalance(
    startDate: string,
    endDate: string,
    branchId?: number
  ): Observable<ITrialBalanceSummary> {
    let params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);

    if (branchId) {
      params = params.set('branchId', branchId.toString());
    }

    return this.http.get<ITrialBalanceSummary>(this.apiUrl, { params });
  }
}
