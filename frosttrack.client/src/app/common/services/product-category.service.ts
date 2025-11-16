import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IProductCategoryListResponse,
  IProductCategoryRequest,
  IProductCategoryResponse,
} from '../models/product-category.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class ProductCategoryService extends BaseService {
  path: string = `${environment.apiUrl}/productcategory`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IProductCategoryListResponse>> {
    return this.get<PaginationResult<IProductCategoryListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Product Categories pagination'
    );
  }

  getById(id: number): Observable<IProductCategoryResponse> {
    return this.get<IProductCategoryResponse>(
      this.path + '/' + id,
      'Load Product Category'
    );
  }

  create(
    payload: IProductCategoryRequest
  ): Observable<IProductCategoryResponse> {
    return this.postWithSuccess<IProductCategoryResponse>(
      this.path,
      payload,
      'Create Product Category',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: IProductCategoryRequest
  ): Observable<IProductCategoryResponse> {
    return this.putWithSuccess<IProductCategoryResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Product Category',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Product Category',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Product Categories',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Product Category Lookup'
    );
  }
}
