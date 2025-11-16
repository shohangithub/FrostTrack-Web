import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IBaseUnitListResponse,
  IBaseUnitRequest,
  IBaseUnitResponse,
} from '../models/base-unit.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class BaseUnitService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/baseunit`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IBaseUnitListResponse>> {
    return this.get<PaginationResult<IBaseUnitListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Base Units pagination'
    );
  }

  getById(id: number): Observable<IBaseUnitResponse> {
    return this.get<IBaseUnitResponse>(this.path + '/' + id, 'Load Base Unit');
  }

  create(payload: IBaseUnitRequest): Observable<IBaseUnitResponse> {
    return this.postWithSuccess<IBaseUnitResponse>(
      this.path,
      payload,
      'Create Base Unit',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IBaseUnitRequest): Observable<IBaseUnitResponse> {
    return this.putWithSuccess<IBaseUnitResponse>(
      this.path + '/' + id,
      payload,
      'Update Base Unit',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Base Unit',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/DeleteBatch',
      ids,
      `Delete ${ids.length} Base Units`,
      MessageHub.DELETE_BATCH
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Base Units Lookup'
    );
  }
}
