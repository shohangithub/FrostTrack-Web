import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IBranchListResponse,
  IBranchRequest,
  IBranchResponse,
} from '../models/branch.interface';
import { CodeResponse } from '../../core/models/code-response';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class BranchService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/branch`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IBranchListResponse>> {
    return this.get<PaginationResult<IBranchListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Branches pagination'
    );
  }

  getList(): Observable<IBranchListResponse[]> {
    return this.get<IBranchListResponse[]>(this.path, 'Load Branch List');
  }

  getById(id: number): Observable<IBranchResponse> {
    return this.get<IBranchResponse>(this.path + '/' + id, 'Load Branch');
  }

  create(payload: IBranchRequest): Observable<IBranchResponse> {
    return this.postWithSuccess<IBranchResponse>(
      this.path,
      payload,
      'Create Branch',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IBranchRequest): Observable<IBranchResponse> {
    return this.putWithSuccess<IBranchResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Branch',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Branch',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Branches',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  generateCode(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-code',
      'Generate Branch Code'
    );
  }
}
