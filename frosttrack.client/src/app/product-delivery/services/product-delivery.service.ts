import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '@core/models/pagination-result';
import { PaginationQuery } from '@core/models/pagination-query';
import {
  IProductDeliveryListResponse,
  IProductDeliveryRequest,
  IProductDeliveryResponse,
} from '../models/product-delivery.interface';
import { CodeResponse } from '@core/models/code-response';
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';

@Injectable({ providedIn: 'root' })
export class ProductDeliveryService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/productdelivery`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IProductDeliveryListResponse>> {
    return this.get<PaginationResult<IProductDeliveryListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`)
    );
  }

  getById(id: number): Observable<IProductDeliveryResponse> {
    return this.get<IProductDeliveryResponse>(this.path + '/' + id);
  }

  create(
    payload: IProductDeliveryRequest
  ): Observable<IProductDeliveryResponse> {
    return this.post<IProductDeliveryResponse>(this.path, payload);
  }

  update(
    id: number,
    payload: IProductDeliveryRequest
  ): Observable<IProductDeliveryResponse> {
    return this.put<IProductDeliveryResponse>(this.path + '/' + id, payload);
  }

  remove(id: number): Observable<boolean> {
    return this.delete<boolean>(this.path + '/' + id);
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.post<boolean>(this.path + '/DeleteBatch', ids);
  }

  generateDeliveryNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(this.path + '/generate-delivery-number');
  }

  getCustomerStockByCustomerId(customerId: number): Observable<any[]> {
    return this.get<any[]>(this.path + '/customer-stock/' + customerId);
  }
}
