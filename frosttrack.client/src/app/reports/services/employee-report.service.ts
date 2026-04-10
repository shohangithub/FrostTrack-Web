import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IEmployeeReportItem } from '../models/employee-report.interface';

@Injectable({
  providedIn: 'root',
})
export class EmployeeReportService {
  private apiUrl = `${environment.apiUrl}/EmployeeReport`;

  constructor(private http: HttpClient) {}

  getEmployeeReport(
    department?: string,
    designation?: string,
    employmentType?: string,
    isActive?: boolean,
  ): Observable<IEmployeeReportItem[]> {
    let params = new HttpParams();

    if (department) {
      params = params.set('department', department);
    }

    if (designation) {
      params = params.set('designation', designation);
    }

    if (employmentType) {
      params = params.set('employmentType', employmentType);
    }

    if (isActive !== undefined && isActive !== null) {
      params = params.set('isActive', isActive.toString());
    }

    return this.http.get<IEmployeeReportItem[]>(this.apiUrl, { params });
  }
}
