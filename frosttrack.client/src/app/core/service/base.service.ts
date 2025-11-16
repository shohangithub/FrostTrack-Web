import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ErrorHandlerService } from './error-handler.service';

@Injectable()
export abstract class BaseService {
  constructor(
    protected http: HttpClient,
    protected errorHandler: ErrorHandlerService
  ) {}

  /**
   * Handle HTTP errors with optional context
   */
  protected handleError(context: string = 'Operation') {
    return (error: HttpErrorResponse): Observable<never> => {
      this.errorHandler.handleComponentError(error, context);
      return throwError(() => error);
    };
  }

  /**
   * Execute HTTP request with automatic error handling
   */
  protected executeRequest<T>(
    request: Observable<T>,
    context: string = 'API Request'
  ): Observable<T> {
    return request.pipe(catchError(this.handleError(context)));
  }

  /**
   * Execute HTTP GET with automatic error handling
   */
  protected get<T>(url: string, context: string = 'Get data'): Observable<T> {
    return this.executeRequest(this.http.get<T>(url), context);
  }

  /**
   * Execute HTTP POST with automatic error handling
   */
  protected post<T>(
    url: string,
    body: any,
    context: string = 'Save data'
  ): Observable<T> {
    return this.executeRequest(this.http.post<T>(url, body), context);
  }

  /**
   * Execute HTTP PUT with automatic error handling
   */
  protected put<T>(
    url: string,
    body: any,
    context: string = 'Update data'
  ): Observable<T> {
    return this.executeRequest(this.http.put<T>(url, body), context);
  }

  /**
   * Execute HTTP DELETE with automatic error handling
   */
  protected delete<T>(
    url: string,
    context: string = 'Delete data'
  ): Observable<T> {
    return this.executeRequest(this.http.delete<T>(url), context);
  }

  /**
   * Execute HTTP PATCH with automatic error handling
   */
  protected patch<T>(
    url: string,
    body: any,
    context: string = 'Update data'
  ): Observable<T> {
    return this.executeRequest(this.http.patch<T>(url, body), context);
  }

  /**
   * Execute HTTP POST with automatic error handling and success message
   */
  protected postWithSuccess<T>(
    url: string,
    body: any,
    context: string,
    successMessage: string
  ): Observable<T> {
    return this.post<T>(url, body, context).pipe(
      tap(() => this.showSuccess(successMessage))
    );
  }

  /**
   * Execute HTTP PUT with automatic error handling and success message
   */
  protected putWithSuccess<T>(
    url: string,
    body: any,
    context: string,
    successMessage: string
  ): Observable<T> {
    return this.put<T>(url, body, context).pipe(
      tap(() => this.showSuccess(successMessage))
    );
  }

  /**
   * Execute HTTP DELETE with automatic error handling and success message
   */
  protected deleteWithSuccess<T>(
    url: string,
    context: string,
    successMessage: string
  ): Observable<T> {
    return this.delete<T>(url, context).pipe(
      tap(() => this.showSuccess(successMessage))
    );
  }

  /**
   * Show success message after successful operation
   */
  protected showSuccess(message: string, title?: string): void {
    this.errorHandler.showSuccess(message, title);
  }

  /**
   * Show warning message
   */
  protected showWarning(message: string, title?: string): void {
    this.errorHandler.showWarning(message, title);
  }

  /**
   * Show info message
   */
  protected showInfo(message: string, title?: string): void {
    this.errorHandler.showInfo(message, title);
  }
}
