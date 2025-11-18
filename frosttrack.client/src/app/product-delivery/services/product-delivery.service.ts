import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '@core/models/pagination-result';
import { PaginationQuery } from '@core/models/pagination-query';
import {
  IDeliveryListResponse,
  IDeliveryRequest,
  IDeliveryResponse,
  ICustomerStockResponse,
} from '../models/product-delivery.interface';
import { CodeResponse } from '@core/models/code-response';
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';

@Injectable({ providedIn: 'root' })
export class DeliveryService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/delivery`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IDeliveryListResponse>> {
    return this.get<PaginationResult<IDeliveryListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`)
    );
  }

  getById(id: string): Observable<IDeliveryResponse> {
    return this.get<IDeliveryResponse>(this.path + '/' + id);
  }

  create(payload: IDeliveryRequest): Observable<IDeliveryResponse> {
    return this.post<IDeliveryResponse>(this.path, payload);
  }

  update(id: string, payload: IDeliveryRequest): Observable<IDeliveryResponse> {
    return this.put<IDeliveryResponse>(this.path + '/' + id, payload);
  }

  remove(id: string): Observable<boolean> {
    return this.delete<boolean>(this.path + '/' + id);
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.post<boolean>(this.path + '/DeleteBatch', ids);
  }

  generateDeliveryNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(this.path + '/generate-delivery-number');
  }

  getCustomerStockByCustomerId(
    customerId: number
  ): Observable<ICustomerStockResponse[]> {
    return this.get<ICustomerStockResponse[]>(
      this.path + '/customer-stock/' + customerId
    );
  }
}
