import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IPaymentMethodListResponse,
  IPaymentMethodRequest,
  IPaymentMethodResponse,
} from '../models/payment-method.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import { CodeResponse } from '@core/models/code-response';

@Injectable({ providedIn: 'root' })
export class PaymentMethodService extends BaseService {
  path: string = `${environment.apiUrl}/paymentmethod`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IPaymentMethodListResponse>> {
    return this.get<PaginationResult<IPaymentMethodListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Payment Methods'
    );
  }

  getById(id: number): Observable<IPaymentMethodResponse> {
    return this.get<IPaymentMethodResponse>(
      this.path + '/' + id,
      'Load Payment Method'
    );
  }

  create(payload: IPaymentMethodRequest): Observable<IPaymentMethodResponse> {
    return this.postWithSuccess<IPaymentMethodResponse>(
      this.path,
      payload,
      'Create Payment Method',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: IPaymentMethodRequest
  ): Observable<IPaymentMethodResponse> {
    return this.putWithSuccess<IPaymentMethodResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Payment Method',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Payment Method',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Payment Methods',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Payment Method Lookup'
    );
  }

  getActiveList(): Observable<IPaymentMethodListResponse[]> {
    return this.get<IPaymentMethodListResponse[]>(
      this.path + '/active',
      'Load Active Payment Methods'
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Generate Payment Method Code'
    );
  }
}
