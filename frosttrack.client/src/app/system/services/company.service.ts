import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import {
  ICompanyListResponse,
  ICompanyRequest,
  ICompanyResponse,
} from '../models/company.interface';
import { BaseService } from '../../core/service/base.service';
import { ErrorHandlerService } from '../../core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class CompanyService extends BaseService {
  constructor(
    httpClient: HttpClient,
    errorHandlerService: ErrorHandlerService
  ) {
    super(httpClient, errorHandlerService);
  }

  path: string = `${environment.apiUrl}/company`;

  getWithPagination(
    pagination: PaginationQuery
  ): Observable<PaginationResult<ICompanyListResponse>> {
    return this.get<PaginationResult<ICompanyListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Companies pagination'
    );
  }

  getList(): Observable<ICompanyListResponse[]> {
    return this.get<ICompanyListResponse[]>(this.path, 'Load Company List');
  }

  getById(id: number): Observable<ICompanyResponse> {
    return this.get<ICompanyResponse>(this.path + '/' + id, 'Load Company');
  }

  create(payload: ICompanyRequest): Observable<ICompanyResponse> {
    return this.postWithSuccess<ICompanyResponse>(
      this.path,
      payload,
      'Create Company',
      MessageHub.ADD
    );
  }

  update(id: number, payload: ICompanyRequest): Observable<ICompanyResponse> {
    return this.putWithSuccess<ICompanyResponse>(
      this.path + '/' + id,
      payload,
      'Update Company',
      MessageHub.UPDATE
    );
  }

  deleteCompany(id: number): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Company',
      MessageHub.DELETE
    );
  }
}
