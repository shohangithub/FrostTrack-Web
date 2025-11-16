import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IDamageListResponse,
  IDamageRequest,
  IDamageResponse,
} from '../models/damage.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import { CodeResponse } from '@core/models/code-response';

@Injectable({ providedIn: 'root' })
export class DamageService extends BaseService {
  path: string = `${environment.apiUrl}/damage`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IDamageListResponse>> {
    return this.get<PaginationResult<IDamageListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Damage pagination'
    );
  }

  getById(id: number): Observable<IDamageResponse> {
    return this.get<IDamageResponse>(this.path + '/' + id, 'Load Damage');
  }

  create(payload: IDamageRequest): Observable<IDamageResponse> {
    return this.postWithSuccess<IDamageResponse>(
      this.path,
      payload,
      'Create Damage',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IDamageRequest): Observable<IDamageResponse> {
    return this.putWithSuccess<IDamageResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Damage',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Damage',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Damages',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Damage Lookup'
    );
  }

  generateCode(isGlobal: boolean = false): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      `${this.path}/generate-code?isGlobal=${isGlobal}`,
      'Damage Code Generation'
    );
  }
}
