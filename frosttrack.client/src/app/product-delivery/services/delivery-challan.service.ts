import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { getApiEndpoint } from 'app/utils/api-builder';
import { PaginationResult } from '@core/models/pagination-result';
import { PaginationQuery } from '@core/models/pagination-query';
import {
  IDeliveryChallanListResponse,
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
    pagination: PaginationQuery,
  ): Observable<PaginationResult<IDeliveryChallanListResponse>> {
    return this.get<PaginationResult<IDeliveryChallanListResponse>>(
      getApiEndpoint(pagination, this.path + `/get-with-pagination`),
      'Load Delivery Challans',
    );
  }

  getList(): Observable<IDeliveryChallanListResponse[]> {
    return this.get<IDeliveryChallanListResponse[]>(
      this.path + '/list',
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
    return this.putWithSuccess<IDeliveryChallanResponse>(
      `${this.path}/${id}/status`,
      { status },
      'Update Challan Status',
      'Status updated successfully',
    );
  }
}
