import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';
import { IRoleRequest, IRoleResponse } from '../models/role.interface';

@Injectable({ providedIn: 'root' })
export class RoleService {
  constructor(private httpClient: HttpClient) {}
  path: string = `${environment.apiUrl}/security`;

  list(): Observable<IRoleResponse[]> {
    return this.httpClient.get<IRoleResponse[]>(this.path + '/roles');
  }

  post(payload: IRoleRequest): Observable<IRoleResponse> {
    return this.httpClient.post<IRoleResponse>(this.path + '/roles', payload);
  }

  delete(name: string): Observable<boolean> {
    return this.httpClient.delete<boolean>(
      this.path + `/roles/${encodeURIComponent(name)}`
    );
  }

  put(id: number, payload: IRoleRequest): Observable<IRoleResponse> {
    return this.httpClient.put<IRoleResponse>(
      this.path + `/roles/${id}`,
      payload
    );
  }

  batchDelete(names: string[]): Observable<boolean> {
    return this.httpClient.post<boolean>(
      this.path + '/roles/batch-delete',
      names
    );
  }
}
