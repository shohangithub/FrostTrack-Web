import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'environments/environment';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AssignClaimService {
  constructor(private httpClient: HttpClient) {}
  path: string = `${environment.apiUrl}/assignclaim`;

  addClaim(
    userId: number,
    claim: { key: string; value: string }
  ): Observable<any> {
    return this.httpClient.post(this.path + `/users/${userId}/claims`, {
      key: claim.key,
      value: claim.value,
    });
  }

  removeClaim(
    userId: number,
    claim: { key: string; value: string }
  ): Observable<any> {
    return this.httpClient.request(
      'DELETE',
      this.path + `/users/${userId}/claims`,
      { body: { key: claim.key, value: claim.value } }
    );
  }

  // Get all users with their claims
  getAllUserClaims(): Observable<
    Array<{
      id: number;
      userName: string;
      email: string;
      claims: Array<{ type: string; value: string }>;
    }>
  > {
    return this.httpClient.get<
      Array<{
        id: number;
        userName: string;
        email: string;
        claims: Array<{ type: string; value: string }>;
      }>
    >(this.path + '/users/claims');
  }
}
