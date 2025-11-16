import { AuthService } from '../service/auth.service';
import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private authenticationService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        this.handleError(error, request);
        return throwError(() => error);
      })
    );
  }

  private handleError(
    error: HttpErrorResponse,
    request: HttpRequest<any>
  ): void {
    console.error('HTTP Error:', error);

    // Check if component wants to handle errors themselves
    const skipInterceptorToast =
      request.headers.get('X-Skip-Error-Toast') === 'true';

    switch (error.status) {
      case 401:
        // Always handle 401 - critical for auth
        this.handle401Unauthorized(error);
        break;
      case 302:
        // Always handle 302 - unexpected redirects
        this.handle302Found(error);
        break;
      case 403:
        // Only show toast for 403 if not suppressed (most 403s are business logic)
        if (!skipInterceptorToast) {
          this.handle403Forbidden(error, false); // Don't show toast by default
        }
        break;
      case 404:
        // Log but don't show toast (often expected in business logic)
        console.log('📭 404 Not Found - Resource not found');
        break;
      case 500:
        // Always handle 500 - critical server errors
        this.handle500InternalServerError(error);
        break;
      case 0:
        // Always handle network errors - critical
        this.handleNetworkError(error);
        break;
      default:
        // Only show toast for server errors (5xx), not client errors
        if (error.status >= 500) {
          this.handleGenericError(error);
        } else {
          console.log(
            `❌ HTTP Error ${error.status} (handled silently):`,
            error
          );
        }
        break;
    }
  }

  private handle401Unauthorized(_error: HttpErrorResponse): void {
    console.log('🚫 401 Unauthorized - Logging out user');

    // Show error message
    this.toastr.error(
      'Your session has expired. Please log in again.',
      'Authentication Required',
      {
        timeOut: 5000,
        closeButton: true,
        progressBar: true,
      }
    );

    // Clear user session and redirect to login
    this.authenticationService.logout();
    this.router.navigate(['/authentication/signin'], {
      queryParams: { returnUrl: this.router.url },
    });
  }

  private handle302Found(_error: HttpErrorResponse): void {
    console.log('🔄 302 Found - Unexpected redirect detected');

    this.toastr.warning(
      'The server attempted to redirect your request. This may indicate an authentication issue.',
      'Unexpected Redirect',
      {
        timeOut: 7000,
        closeButton: true,
        progressBar: true,
      }
    );

    // If this happens, it might be an authentication issue
    // Check if user is still authenticated
    if (!this.authenticationService.currentUserValue?.token) {
      this.authenticationService.logout();
      this.router.navigate(['/authentication/signin']);
    }
  }

  private handle403Forbidden(
    error: HttpErrorResponse,
    showToast: boolean = true
  ): void {
    console.log('🔒 403 Forbidden - Access denied');

    // Only show toast if allowed (for critical system-level 403s)
    if (showToast) {
      const message =
        error.error?.message ||
        'You do not have permission to access this resource.';

      this.toastr.error(message, 'Access Forbidden', {
        timeOut: 6000,
        closeButton: true,
        progressBar: true,
      });
    }

    // Optionally redirect to dashboard or previous page
    // this.router.navigate(['/dashboard']);
  }

  private handle404NotFound(error: HttpErrorResponse): void {
    console.log('📭 404 Not Found - Resource not found');

    const message =
      error.error?.message || 'The requested resource was not found.';

    this.toastr.error(message, 'Resource Not Found', {
      timeOut: 5000,
      closeButton: true,
      progressBar: true,
    });

    // For API calls, don't redirect. For page navigation, redirect to 404 page
    if (this.isPageNavigation(error.url || undefined)) {
      this.router.navigate(['/authentication/page404']);
    }
  }

  private handle500InternalServerError(_error: HttpErrorResponse): void {
    console.log('💥 500 Internal Server Error');

    this.toastr.error(
      'An internal server error occurred. Please try again later.',
      'Server Error',
      {
        timeOut: 8000,
        closeButton: true,
        progressBar: true,
      }
    );
  }

  private handleNetworkError(_error: HttpErrorResponse): void {
    console.log('🌐 Network Error - No internet connection');

    this.toastr.error(
      'Unable to connect to the server. Please check your internet connection.',
      'Network Error',
      {
        timeOut: 10000,
        closeButton: true,
        progressBar: true,
      }
    );
  }

  private handleGenericError(error: HttpErrorResponse): void {
    console.log(`❌ HTTP Error ${error.status}:`, error);

    const message =
      error.error?.message ||
      error.message ||
      `An unexpected error occurred (${error.status})`;

    this.toastr.error(message, `Error ${error.status}`, {
      timeOut: 6000,
      closeButton: true,
      progressBar: true,
    });
  }

  private isPageNavigation(url?: string): boolean {
    // Check if this is a page navigation vs API call
    return url ? !url.includes('/api/') : false;
  }
}
