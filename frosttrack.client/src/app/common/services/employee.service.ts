import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  IEmployeeListResponse,
  IEmployeeRequest,
  IEmployeeResponse,
} from '../models/employee.interface';
import { ILookup } from '../../core/models/lookup';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';
import { CodeResponse } from '@core/models/code-response';

@Injectable({ providedIn: 'root' })
export class EmployeeService extends BaseService {
  path: string = `${environment.apiUrl}/employee`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<IEmployeeListResponse>> {
    return this.get<PaginationResult<IEmployeeListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Employees pagination'
    );
  }

  getById(id: number): Observable<IEmployeeResponse> {
    return this.get<IEmployeeResponse>(this.path + '/' + id, 'Load Employee');
  }

  create(payload: IEmployeeRequest): Observable<IEmployeeResponse> {
    return this.postWithSuccess<IEmployeeResponse>(
      this.path,
      payload,
      'Create Employee',
      MessageHub.ADD
    );
  }

  update(id: number, payload: IEmployeeRequest): Observable<IEmployeeResponse> {
    return this.putWithSuccess<IEmployeeResponse>(
      `${this.path}/${id}`,
      payload,
      'Update Employee',
      MessageHub.UPDATE
    );
  }

  remove(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      `${this.path}/${id}`,
      'Delete Employee',
      MessageHub.DELETE_ONE
    );
  }

  batchDelete(ids: number[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      `${this.path}/DeleteBatch`,
      ids,
      'Delete Employees',
      `${ids.length} ${MessageHub.DELETE_BATCH}`
    );
  }

  getLookup(): Observable<ILookup<number>[]> {
    return this.get<ILookup<number>[]>(
      this.path + `/lookup`,
      'Load Employee Lookup'
    );
  }

  generateCode(isGlobal: boolean = false): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      `${this.path}/generate-code?isGlobal=${isGlobal}`,
      'Employee Code Generation'
    );
  }

  getDistinctDepartments(): Observable<string[]> {
    return this.get<string[]>(`${this.path}/departments`, 'Load Departments');
  }

  getDistinctDesignations(): Observable<string[]> {
    return this.get<string[]>(`${this.path}/designations`, 'Load Designations');
  }
}
