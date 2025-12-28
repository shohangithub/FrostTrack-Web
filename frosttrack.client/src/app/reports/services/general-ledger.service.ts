import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { IGeneralLedgerReport } from '../models/general-ledger.interface';

@Injectable({
  providedIn: 'root',
})
export class GeneralLedgerService {
  private apiUrl = `${environment.apiUrl}/GeneralLedger`;

  constructor(private http: HttpClient) {}

  getGeneralLedger(reportDate: Date): Observable<IGeneralLedgerReport> {
    let params = new HttpParams();
    params = params.append('reportDate', reportDate.toISOString());

    return this.http.get<IGeneralLedgerReport>(this.apiUrl, { params });
  }
}
