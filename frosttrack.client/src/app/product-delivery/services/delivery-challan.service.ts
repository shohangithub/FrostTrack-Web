import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '@core/models/pagination-result';
import {
  IDeliveryChallanListResponse,
  IDeliveryChallanPaginationQuery,
  IDeliveryChallanRequest,
  IDeliveryChallanResponse,
} from '../models/delivery-challan.interface';
import { CodeResponse } from '@core/models/code-response';
import { BaseService } from '@core/service/base.service';
import { ErrorHandlerService } from '@core/service/error-handler.service';
import { MessageHub } from '@config/message-hub';

@Injectable({ providedIn: 'root' })
export class DeliveryChallanService extends BaseService {
  path: string = `${environment.apiUrl}/DeliveryChallan`;

  constructor(http: HttpClient, errorHandler: ErrorHandlerService) {
    super(http, errorHandler);
  }

  getWithPagination(
    pagination: IDeliveryChallanPaginationQuery,
  ): Observable<PaginationResult<IDeliveryChallanListResponse>> {
    return this.get<PaginationResult<IDeliveryChallanListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Delivery Challans',
    );
  }

  getList(
    status: 'active' | 'archived' | 'deleted' = 'active',
  ): Observable<IDeliveryChallanListResponse[]> {
    return this.get<IDeliveryChallanListResponse[]>(
      `${this.path}/list?status=${status}`,
      'Load Delivery Challan List',
    );
  }

  getById(id: string): Observable<IDeliveryChallanResponse> {
    return this.get<IDeliveryChallanResponse>(
      this.path + '/' + id,
      'Load Delivery Challan',
    );
  }

  create(
    payload: IDeliveryChallanRequest,
  ): Observable<IDeliveryChallanResponse> {
    return this.postWithSuccess<IDeliveryChallanResponse>(
      this.path,
      payload,
      'Create Delivery Challan',
      MessageHub.ADD,
    );
  }

  update(
    id: string,
    payload: IDeliveryChallanRequest,
  ): Observable<IDeliveryChallanResponse> {
    return this.putWithSuccess<IDeliveryChallanResponse>(
      this.path + '/' + id,
      payload,
      'Update Delivery Challan',
      MessageHub.UPDATE,
    );
  }

  remove(id: string): Observable<boolean> {
    return this.deleteWithSuccess<boolean>(
      this.path + '/' + id,
      'Delete Delivery Challan',
      MessageHub.DELETE_ONE,
    );
  }

  batchDelete(ids: string[]): Observable<boolean> {
    return this.postWithSuccess<boolean>(
      this.path + '/delete-batch',
      ids,
      'Delete Delivery Challans',
      `${ids.length} ${MessageHub.DELETE_BATCH}`,
    );
  }

  softDelete(id: string): Observable<void> {
    return this.post<void>(`${this.path}/${id}/soft-delete`, {});
  }

  restore(id: string): Observable<void> {
    return this.post<void>(`${this.path}/${id}/restore`, {});
  }

  archive(id: string): Observable<void> {
    return this.post<void>(`${this.path}/${id}/archive`, {});
  }

  unarchive(id: string): Observable<void> {
    return this.post<void>(`${this.path}/${id}/unarchive`, {});
  }

  generateChallanNumber(): Observable<CodeResponse> {
    return this.get<CodeResponse>(
      this.path + '/generate-challan-number',
      'Generate Challan Number',
    );
  }

  updateStatus(
    id: string,
    status: string,
  ): Observable<IDeliveryChallanResponse> {
    return this.patch<IDeliveryChallanResponse>(
      `${this.path}/${id}/status`,
      { status },
      'Update Challan Status',
    );
  }
}
