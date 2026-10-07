import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { ICustomerBillStatement } from '../models/customer-bill-statement.interface';

@Injectable({
  providedIn: 'root',
})
export class CustomerBillStatementService {
  private apiUrl = `${environment.apiUrl}/CustomerBillStatement`;

  constructor(private http: HttpClient) {}

  /** fromDate / toDate are local calendar days in yyyy-MM-dd form. */
  getStatement(
    customerId: number,
    fromDate: string,
    toDate: string,
  ): Observable<ICustomerBillStatement> {
    const params = new HttpParams()
      .set('customerId', customerId.toString())
      .set('fromDate', fromDate)
      .set('toDate', toDate);

    return this.http.get<ICustomerBillStatement>(this.apiUrl, { params });
  }
}
