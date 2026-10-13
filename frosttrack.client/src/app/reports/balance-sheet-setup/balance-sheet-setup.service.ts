import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'environments/environment';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import {
  IBalanceSheetEntry,
  IBalanceSheetEntryRequest,
  IBalanceSheetSettings,
} from './balance-sheet-setup.interfaces';

@Injectable({ providedIn: 'root' })
export class BalanceSheetSetupService extends BaseService {
  path: string = `${environment.apiUrl}/BalanceSheetEntry`;

  constructor(httpClient: HttpClient, errorHandlerService: ErrorHandlerService) {
    super(httpClient, errorHandlerService);
  }

  getAll(): Observable<IBalanceSheetEntry[]> {
    return this.get<IBalanceSheetEntry[]>(this.path, 'Load Opening Balances');
  }

  create(payload: IBalanceSheetEntryRequest): Observable<IBalanceSheetEntry> {
    return this.postWithSuccess<IBalanceSheetEntry>(
      this.path,
      payload,
      'Create Opening Balance',
      MessageHub.ADD
    );
  }

  update(id: string, payload: IBalanceSheetEntryRequest): Observable<IBalanceSheetEntry> {
    return this.putWithSuccess<IBalanceSheetEntry>(
      `${this.path}/${id}`,
      payload,
      'Update Opening Balance',
      MessageHub.UPDATE
    );
  }

  remove(id: string): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Opening Balance',
      MessageHub.DELETE_ONE
    );
  }

  getSettings(): Observable<IBalanceSheetSettings> {
    return this.get<IBalanceSheetSettings>(`${this.path}/settings`, 'Load Balance Sheet Settings');
  }

  updateSettings(payload: IBalanceSheetSettings): Observable<IBalanceSheetSettings> {
    return this.putWithSuccess<IBalanceSheetSettings>(
      `${this.path}/settings`,
      payload,
      'Save Balance Sheet Settings',
      MessageHub.UPDATE
    );
  }
}
