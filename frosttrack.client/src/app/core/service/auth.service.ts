import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError, interval } from 'rxjs';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { LoginRequest } from '@core/models/login-request';
import { TokenResponse } from '@core/models/token-response';
import { environment } from 'environments/environment';
import { JwtHelperService } from 'angular-jwt-updated';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  public currentUserSubject: BehaviorSubject<TokenResponse>;
  public currentUser: Observable<TokenResponse>;
  private selectedBranchId: BehaviorSubject<number>;
  private jwtHelper = new JwtHelperService();
  private tokenCheckInterval: any;

  private users = [
    {
      id: 1,
      name: 'Admin User',
      username: 'admin@email.com',
      password: 'admin@123',
      firstName: 'Sarah',
      lastName: 'Smith',
      token: 'admin-token',
    },
  ];
  path: string = `${environment.apiUrl}/login`;

  constructor(private httpClient: HttpClient, private router: Router) {
    this.currentUserSubject = new BehaviorSubject<TokenResponse>(
      JSON.parse(localStorage.getItem('currentUser') || '{}')
    );
    this.currentUser = this.currentUserSubject.asObservable();
    this.selectedBranchId = new BehaviorSubject<number>(0);
    this.setBranchId();

    // Start periodic token expiration check
    this.startTokenExpirationCheck();
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
        name: user.name,
        email: user.username,
        token: user.token,
      });
    }
  }
  ok(body?: { id: number; name: string; email: string; token: string }) {
    return of(new HttpResponse({ status: 200, body }));
  }
  error(message: string) {
    return throwError(message);
  }

  logout() {
    // Clear token expiration check
    if (this.tokenCheckInterval) {
      clearInterval(this.tokenCheckInterval);
    }

    // remove user from local storage to log user out
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(this.currentUserValue);
    return of({ success: false });
  }

  /**
   * Start periodic check for token expiration
   * Checks every 60 seconds if token is expired
   */
  private startTokenExpirationCheck(): void {
    // Check immediately
    this.checkTokenExpiration();

    // Then check every 60 seconds
    this.tokenCheckInterval = setInterval(() => {
      this.checkTokenExpiration();
    }, 60000); // Check every minute
  }

  /**
   * Check if current token is expired and logout if needed
   */
  private checkTokenExpiration(): void {
    const currentUser = this.currentUserValue;

    if (currentUser && currentUser.token) {
      try {
        const isExpired = this.jwtHelper.isTokenExpired(currentUser.token);

        if (isExpired) {
          console.log('🔒 Token expired, logging out automatically');
          this.logout();
          this.router.navigate(['/authentication/signin'], {
            queryParams: { reason: 'expired' },
          });
        }
      } catch (error) {
        console.error('Error checking token expiration:', error);
      }
    }
  }

  /**
   * Check if user is currently authenticated with a valid token
   */
  isAuthenticated(): boolean {
    const currentUser = this.currentUserValue;
    if (!currentUser || !currentUser.token) {
      return false;
    }

    try {
      return !this.jwtHelper.isTokenExpired(currentUser.token);
    } catch {
      return false;
    }
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

  getUserId(): number | null {
    const helper = new JwtHelperService();
    const tokenObj = this.currentUserSubject.value;
    if (tokenObj && tokenObj.token) {
      const decodedToken = helper.decodeToken(tokenObj.token);
      return (
        decodedToken['sub'] ||
        decodedToken['userId'] ||
        decodedToken[
          'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
        ] ||
        null
      );
    }
    return null;
  }

  getDecodedToken(): any {
    const helper = new JwtHelperService();
    const tokenObj = this.currentUserSubject.value;
    if (tokenObj && tokenObj.token) {
      return helper.decodeToken(tokenObj.token);
    }
    return null;
  }
}
