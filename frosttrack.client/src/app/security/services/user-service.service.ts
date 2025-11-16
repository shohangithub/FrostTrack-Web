import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import {
  IUserListResponse,
  IUserRequest,
  IUserResponse,
} from '../models/user.interface';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class UserService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }
  path: string = `${environment.apiUrl}/users`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IUserListResponse>> {
    return this.get<PaginationResult<IUserListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Users pagination'
    );
  }

  getById(id: number): Observable<IUserResponse> {
    return this.get<IUserResponse>(this.path + '/' + id, 'Load User');
  }

  create(payload: IUserRequest): Observable<IUserResponse> {
    return this.postWithSuccess<IUserResponse>(
      this.path,
      payload,
      'Create User',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IUserRequest): Observable<IUserResponse> {
    return this.putWithSuccess<IUserResponse>(
      `${this.path}/${id}`,
      payload,
      'Update User',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete User',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Users',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  // Assign role to existing user
  postRole(userId: number, role: string) {
    return this.putWithSuccess(
      this.path + `/${userId}/roles`,
      role,
      'Assign Role to User',
      MessageHub.ADD
    );
  }

  removeRole(userId: number, role: string) {
    return this.deleteWithSuccess(
      this.path + `/${userId}/roles?role=${encodeURIComponent(role)}`,
      'Remove Role from User',
      MessageHub.DELETE_ONE
    );
  }
}
