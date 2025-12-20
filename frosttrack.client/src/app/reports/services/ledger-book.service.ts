import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ILedgerBookResponse } from '../interfaces/ledger-book.interface';
import { environment } from 'environments/environment.development';

@Injectable({
  providedIn: 'root',
})
export class LedgerBookService {
  private readonly http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/LedgerBook`;
  getGeneralLedger(reportDate: Date): Observable<ILedgerBookResponse> {
    const params = new HttpParams().set('reportDate', reportDate.toISOString());

    return this.http.get<ILedgerBookResponse>(this.apiUrl, { params });
  }
}
