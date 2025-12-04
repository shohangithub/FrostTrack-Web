import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IDashboardStatsResponse } from '../models/dashboard.interface';
import { environment } from 'environments/environment';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}/Dashboard`;

  constructor(private http: HttpClient) {}

  getDashboardStats(
    periodDays?: number,
    startDate?: string,
    endDate?: string,
    branchId?: number
  ): Observable<IDashboardStatsResponse> {
    let params = new HttpParams();

    if (periodDays) {
      params = params.set('periodDays', periodDays.toString());
    }

    if (startDate) {
      params = params.set('startDate', startDate);
    }

    if (endDate) {
      params = params.set('endDate', endDate);
    }

    if (branchId) {
      params = params.set('branchId', branchId.toString());
    }

    return this.http.get<IDashboardStatsResponse>(this.apiUrl, { params });
  }
}
