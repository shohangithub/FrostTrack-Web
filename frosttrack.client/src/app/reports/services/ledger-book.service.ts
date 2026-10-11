import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import {
  ILedgerBookGroup,
  ILedgerBookSummary,
} from '../interfaces/ledger-book.interface';

@Injectable({
  providedIn: 'root',
})
export class LedgerBookService {
  private readonly http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/LedgerBook`;

  getSummary(fromDate: Date, toDate: Date): Observable<ILedgerBookSummary> {
    return this.http.get<ILedgerBookSummary>(`${this.apiUrl}/summary`, {
      params: this.rangeParams(fromDate, toDate),
    });
  }

  getGroupLedger(
    groupId: string,
    fromDate: Date,
    toDate: Date
  ): Observable<ILedgerBookGroup> {
    return this.http.get<ILedgerBookGroup>(`${this.apiUrl}/group/${groupId}`, {
      params: this.rangeParams(fromDate, toDate),
    });
  }

  private rangeParams(fromDate: Date, toDate: Date): HttpParams {
    return new HttpParams()
      .append('fromDate', fromDate.toISOString())
      .append('toDate', toDate.toISOString());
  }
}
