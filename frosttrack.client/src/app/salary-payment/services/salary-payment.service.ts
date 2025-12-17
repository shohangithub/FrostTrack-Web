import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { PaginationQuery } from '@core/models/pagination-query';
import { PaginationResult } from '@core/models/pagination-result';
import { getApiEndpoint } from 'app/utils/api-builder';
import {
  ISalaryPaymentRequest,
  ISalaryPaymentResponse,
  IEmployeeForSalary,
  ISalaryPaymentList,
  IMonthlyPaymentSummary,
} from '../models/salary-payment.interface';

@Injectable({ providedIn: 'root' })
export class SalaryPaymentService {
  private path: string = `${environment.apiUrl}/SalaryPayment`;

  constructor(private http: HttpClient) {}

  getEmployeesForPayment(): Observable<IEmployeeForSalary[]> {
    return this.http.get<IEmployeeForSalary[]>(`${this.path}/employees`);
  }

  getSalaryPaymentList(): Observable<ISalaryPaymentList[]> {
    return this.http.get<ISalaryPaymentList[]>(`${this.path}/list`);
  }

  getWithPagination(
    pagination: PaginationQuery,
    employeeId?: number,
    month?: number,
    year?: number
  ): Observable<PaginationResult<ISalaryPaymentList>> {
    const paginationParams = {
      pageSize: pagination.pageSize,
      pageIndex: pagination.pageIndex,
      orderBy: pagination.orderBy,
      isAscending: pagination.isAscending,
      openText: pagination.openText,
      employeeId: employeeId && employeeId > 0 ? employeeId : undefined,
      month: month && month > 0 ? month : undefined,
      year: year && year > 0 ? year : undefined,
    };

    const url = getApiEndpoint(
      paginationParams,
      `${this.path}/get-with-pagination`
    );

    return this.http.get<PaginationResult<ISalaryPaymentList>>(url);
  }

  createSalaryPayment(
    request: ISalaryPaymentRequest
  ): Observable<ISalaryPaymentResponse> {
    return this.http.post<ISalaryPaymentResponse>(this.path, request);
  }

  getPaymentHistory(
    employeeId?: number,
    startDate?: Date,
    endDate?: Date
  ): Observable<ISalaryPaymentList[]> {
    let params = new HttpParams();

    if (employeeId) {
      params = params.set('employeeId', employeeId.toString());
    }
    if (startDate) {
      params = params.set('startDate', startDate.toISOString());
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString());
    }

    return this.http.get<ISalaryPaymentList[]>(`${this.path}/history`, {
      params,
    });
  }

  getMonthlyReport(
    month: number,
    year: number
  ): Observable<IMonthlyPaymentSummary> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());

    return this.http.get<IMonthlyPaymentSummary>(
      `${this.path}/monthly-report`,
      { params }
    );
  }

  getById(id: string): Observable<ISalaryPaymentResponse> {
    return this.http.get<ISalaryPaymentResponse>(`${this.path}/${id}`);
  }

  updateSalaryPayment(
    id: string,
    request: ISalaryPaymentRequest
  ): Observable<ISalaryPaymentResponse> {
    return this.http.put<ISalaryPaymentResponse>(`${this.path}/${id}`, request);
  }

  deleteSalaryPayment(transactionId: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.path}/${transactionId}`);
  }
}
