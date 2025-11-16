import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IUnitConversionListResponse,
  IUnitConversionRequest,
  IUnitConversionResponse,
} from '../models/unit-conversion.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class UnitConversionService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/unitconversion`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IUnitConversionListResponse>> {
    return this.get<PaginationResult<IUnitConversionListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Unit Conversions pagination'
    );
  }

  getById(id: number): Observable<IUnitConversionResponse> {
    return this.get<IUnitConversionResponse>(
      this.path + '/' + id,
      'Load Unit Conversion'
    );
  }

  create(payload: IUnitConversionRequest): Observable<IUnitConversionResponse> {
    return this.postWithSuccess<IUnitConversionResponse>(
      this.path,
      payload,
      'Create Unit Conversion',
      MessageHub.ADD
    );
  }

  update(
    id: number,
    payload: IUnitConversionRequest
  ): Observable<IUnitConversionResponse> {
    return this.putWithSuccess<IUnitConversionResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Unit Conversion',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Unit Conversion',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Unit Conversions',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Unit Conversions Lookup'
    );
  }
}
