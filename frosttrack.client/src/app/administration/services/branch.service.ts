import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '../../core/models/pagination-result';
import { PaginationQuery } from '../../core/models/pagination-query';
import { IBranchListResponse, IBranchRequest, IBranchResponse } from '../models/branch.interface';
import { ILookup } from '../../core/models/lookup';
import { CodeResponse } from '../../core/models/code-response';

@Injectable({ providedIn: 'root' })
export class BranchService {
  constructor(private httpClient: HttpClient) { }
  path: string = `${environment.apiUrl}/branch`;

  get(pagination: PaginationQuery): Observable<PaginationResult<IBranchListResponse>> {
    return this.httpClient.get<PaginationResult<IBranchListResponse>>(getApiEndpoint(pagination, this.path + `/get-with-pagination`));
  }

  getById(id: number): Observable<IBranchResponse> {
    return this.httpClient.get<IBranchResponse>(this.path + '/' + id);
  }

  post(payload: IBranchRequest): Observable<IBranchResponse> {
    return this.httpClient.post<IBranchResponse>(this.path, payload);
  }
  put(id: number, payload: IBranchRequest): Observable<IBranchResponse> {
    return this.httpClient.put<IBranchResponse>(this.path + '/' + id, payload);
  }
  delete(id: number): Observable<boolean> {
    return this.httpClient.delete<boolean>(this.path + '/' + id);
  }
  batchDelete(ids: number[]): Observable<boolean> {
    return this.httpClient.post<boolean>(this.path + '/DeleteBatch', ids);
  }
  getLookup(): Observable<ILookup<number>[]> {
    return this.httpClient.get<ILookup<number>[]>(this.path + `/lookup`);
  }

  generateCode(): Observable<CodeResponse> {
    return this.httpClient.get<CodeResponse>(this.path + '/generate-code');
  }

}
