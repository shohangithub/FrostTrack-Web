import { Injectable } from '@angular/core';
import {
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
} from '@angular/router';
import { JwtHelperService } from 'angular-jwt-updated';
import { AuthService } from '../service/auth.service';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard {
  private jwtHelper = new JwtHelperService();

  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {
    const currentUser = this.authService.currentUserValue;

    // Check if user exists and has a token
    if (currentUser && currentUser.token) {
      // Check if token is expired
      const isExpired = this.jwtHelper.isTokenExpired(currentUser.token);

      if (!isExpired) {
        // Token is valid, allow access
        return true;
      } else {
        // Token expired, logout and redirect
        console.log('🔒 Token expired, logging out');
        this.authService.logout();
        this.router.navigate(['/authentication/signin'], {
          queryParams: { returnUrl: state.url, reason: 'expired' },
        });
        return false;
      }
    }

    // No user or token, redirect to login
    this.router.navigate(['/authentication/signin'], {
      queryParams: { returnUrl: state.url },
    });
    return false;
  }
}
