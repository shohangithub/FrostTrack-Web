import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IProductListResponse,
  IProductListWithStockResponse,
  IProductRequest,
  IProductResponse,
} from '../models/product.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class ProductService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/product`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IProductListResponse>> {
    return this.get<PaginationResult<IProductListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Products'
    );
  }

  getList(): Observable<IProductListResponse[]> {
    return this.get<IProductListResponse[]>(
      this.path + `/get-list`,
      'Load Product List'
    );
  }

  getProductsWithoutService(): Observable<IProductListResponse[]> {
    return this.get<IProductListResponse[]>(
      this.path + `/get-list-without-service`,
      'Load Products Without Service'
    );
  }

  getListWithStock(): Observable<IProductListWithStockResponse[]> {
    return this.get<IProductListWithStockResponse[]>(
      this.path + `/get-list-with-stock`,
      'Load Products With Stock'
    );
  }

  getById(id: number): Observable<IProductResponse> {
    return this.get<IProductResponse>(this.path + '/' + id, 'Load Product');
  }

  create(payload: IProductRequest): Observable<IProductResponse> {
    return this.postWithSuccess<IProductResponse>(
      this.path,
      payload,
      'Create Product',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IProductRequest): Observable<IProductResponse> {
    return this.putWithSuccess<IProductResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Product',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Product',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Products',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Product Lookup'
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Product Code Generation'
    );
  }
}
