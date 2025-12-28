import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IBankBookReport } from '../models/bankbook.interface';

@Injectable({
  providedIn: 'root',
})
export class BankBookService {
  private apiUrl = `${environment.apiUrl}/BankBook`;

  constructor(private http: HttpClient) {}

  getBankBook(reportDate: Date): Observable<IBankBookReport> {
    let params = new HttpParams();
    params = params.append('reportDate', reportDate.toISOString());

    return this.http.get<IBankBookReport>(this.apiUrl, { params });
  }
}
