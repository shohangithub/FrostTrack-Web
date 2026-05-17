import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import {
  IRecurringChargePreview,
  IRecurringChargeRunRequest,
  IRecurringChargeRunResponse,
} from '../models/booking.interface';

@Injectable({ providedIn: 'root' })
export class RecurringChargeManagementService extends BaseService {
  private path = `${environment.apiUrl}/RecurringChargeManagement`;

  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService,
  ) {
    super(httpClient, errorHandlerService);
  }

  preview(asOfDate?: string): Observable<IRecurringChargePreview> {
    let params = new HttpParams();
    if (asOfDate) params = params.set('asOfDate', asOfDate);
    return this.http.get<IRecurringChargePreview>(`${this.path}/preview`, { params });
  }

  apply(request: IRecurringChargeRunRequest): Observable<IRecurringChargeRunResponse> {
    return this.http.post<IRecurringChargeRunResponse>(`${this.path}/apply`, request);
  }

  getHistory(take = 30): Observable<IRecurringChargeRunResponse[]> {
    const params = new HttpParams().set('take', take.toString());
    return this.http.get<IRecurringChargeRunResponse[]>(`${this.path}/history`, {
      params,
    });
  }
}
