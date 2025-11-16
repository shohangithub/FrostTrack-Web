import { inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
  HttpResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, finalize } from 'rxjs/operators';
import { LoadingService } from '../service/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const loadingService = inject(LoadingService);

  // Start loading
  loadingService.setLoading(true);

  return next(request).pipe(
    tap((event) => {
      if (event instanceof HttpResponse) {
        // Request completed successfully
      }
    }),
    finalize(() => {
      // Request completed (success or error)
      loadingService.setLoading(false);
    })
  );
};
