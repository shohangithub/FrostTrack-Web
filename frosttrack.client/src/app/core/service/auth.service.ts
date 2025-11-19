import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { User } from '../models/user';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { LoginRequest } from '@core/models/login-request';
import { TokenResponse } from '@core/models/token-response';
import { environment } from 'environments/environment';
import { JwtHelperService } from 'angular-jwt-updated';
import { ErrorResponse } from 'app/utils/server-error-handler';
@Injectable({
  providedIn: 'root',
})
export class AuthService {
  public currentUserSubject: BehaviorSubject<TokenResponse>;
  public currentUser: Observable<TokenResponse>;
  private selectedBranchId: BehaviorSubject<number>;

  private users = [
    {
      id: 1,
      username: 'admin@email.com',
      password: 'admin@123',
      firstName: 'Sarah',
      lastName: 'Smith',
      token: 'admin-token',
    },
  ];
  path: string = `${environment.apiUrl}/login`;
  constructor(private httpClient: HttpClient) {
    this.currentUserSubject = new BehaviorSubject<TokenResponse>(
      JSON.parse(localStorage.getItem('currentUser') || '{}')
    );
    this.currentUser = this.currentUserSubject.asObservable();
    this.selectedBranchId = new BehaviorSubject<number>(0);
    this.setBranchId();
  }

  public get currentUserValue(): TokenResponse {
    return this.currentUserSubject.value;
  }

  public get currentBranchId(): number {
    return this.selectedBranchId.value;
  }

  post(payload: LoginRequest): Observable<TokenResponse> {
    return this.httpClient.post<TokenResponse>(this.path, payload);
  }

  login(username: string, password: string) {
    const user = this.users.find(
      (u) => u.username === username && u.password === password
    );

    if (!user) {
      return this.error('Username or password is incorrect');
    } else {
      localStorage.setItem('currentUser', JSON.stringify(user));
      this.currentUserSubject.next(user);
      this.setBranchId();
      return this.ok({
        id: user.id,
        username: user.username,
        firstName: user.firstName,
        lastName: user.lastName,
        token: user.token,
      });
    }
  }
  ok(body?: {
    id: number;
    username: string;
    firstName: string;
    lastName: string;
    token: string;
  }) {
    return of(new HttpResponse({ status: 200, body }));
  }
  error(message: string) {
    return throwError(message);
  }

  logout() {
    // remove user from local storage to log user out
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(this.currentUserValue);
    return of({ success: false });
  }

  get getCurrentSelectedBranch$() {
    return this.selectedBranchId.asObservable();
  }

  setUserBranchId(branchId: number) {
    this.selectedBranchId.next(branchId);
  }

  getBranchId() {
    const helper = new JwtHelperService();
    const tokenObj = this.currentUserSubject.value;
    if (tokenObj) {
      const decodedToken = helper.decodeToken(tokenObj.token);
      return decodedToken.BranchId;
    }
    return null;
  }

  setBranchId() {
    const helper = new JwtHelperService();
    const tokenObj = this.currentUserSubject.value;
    if (tokenObj) {
      if (tokenObj?.token) {
        const decodedToken = helper.decodeToken(tokenObj?.token);
        this.selectedBranchId.next(decodedToken.BranchId);
      }
    }
  }
  getUserRoles() {
    const helper = new JwtHelperService();
    const tokenObj = this.currentUserSubject.value;
    if (tokenObj) {
      if (tokenObj?.token) {
        const decodedToken = helper.decodeToken(tokenObj?.token);
        return (
          decodedToken['role'] ||
          decodedToken['roles'] ||
          decodedToken[
            'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
          ] ||
          null
        );
      }
    }
  }
}
