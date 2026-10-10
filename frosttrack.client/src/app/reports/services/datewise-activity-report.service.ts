import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import {
  ActivityReportType,
  IDatewiseActivityReport,
} from '../models/datewise-activity-report.interface';

@Injectable({
  providedIn: 'root',
})
export class DatewiseActivityReportService {
  private apiUrl = `${environment.apiUrl}/DatewiseActivityReport`;

  constructor(private http: HttpClient) {}

  /** fromDate / toDate are local calendar days (yyyy-MM-dd); use the same day for a single-date report. */
  getReport(
    type: ActivityReportType,
    fromDate: string,
    toDate: string,
    customerId?: number | null,
  ): Observable<IDatewiseActivityReport> {
    let params = new HttpParams()
      .set('type', type)
      .set('fromDate', fromDate)
      .set('toDate', toDate);
    if (customerId) {
      params = params.set('customerId', customerId.toString());
    }
    return this.http.get<IDatewiseActivityReport>(this.apiUrl, { params });
  }
}
