import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IAssetListResponse,
  IAssetRequest,
  IAssetResponse,
} from '../models/asset.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import { CodeResponse } from '@core/models/code-response';

@Injectable({ providedIn: 'root' })
export class AssetService extends BaseService {
  path: string = `${environment.apiUrl}/asset`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IAssetListResponse>> {
    return this.get<PaginationResult<IAssetListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Assets pagination'
    );
  }

  getById(id: number): Observable<IAssetResponse> {
    return this.get<IAssetResponse>(this.path + '/' + id, 'Load Asset');
  }

  create(payload: IAssetRequest): Observable<IAssetResponse> {
    return this.postWithSuccess<IAssetResponse>(
      this.path,
      payload,
      'Create Asset',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IAssetRequest): Observable<IAssetResponse> {
    return this.putWithSuccess<IAssetResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Asset',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Asset',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Assets',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Asset Lookup'
    );
  }

  generateCode(isGlobal: boolean = false): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      `${this.path}/generate-code?isGlobal=${isGlobal}`,
      'Asset Code Generation'
    );
  }

  getCurrentValue(assetId: number): Observable<number> {
    return this.get<number>(
      `${this.path}/${assetId}/current-value`,
      'Load Asset Current Value'
    );
  }

  getDistinctAssetTypes(): Observable<string[]> {
    return this.get<string[]>(`${this.path}/asset-types`, 'Load Asset Types');
  }
}
