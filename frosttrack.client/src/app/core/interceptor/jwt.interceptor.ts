import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../service/auth.service';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private authenticationService: AuthService) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // add authorization header with jwt token if available

    let currentUser = this.authenticationService.currentUserValue;
    if (currentUser && currentUser.token) {
      request = request.clone({
        setHeaders: {
          //Authorization: `Bearer ${currentUser.token}`,
          Authorization: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJyaXMuc2hvaGFuQGdtYWlsLmNvbSIsIm5hbWUiOiJyaXMuc2hvaGFuQGdtYWlsLmNvbSIsImZhbWlseV9uYW1lIjoicmlzLnNob2hhbkBnbWFpbC5jb20iLCJJZCI6IjEiLCJUZW5hbnQiOiIxMTIyMzM0NC01NTY2LTc3ODgtOTlhYS1iYmNjZGRlZWZmMDAiLCJSb2xlIjpbIkFkbWluIiwiQWRtaW4iLCJTdGFuZGFyZCJdLCJleHAiOjE3MTIwNjA4MzAsImlzcyI6InJpcy5zaG9oYW5AZ21haWwuY29tIiwiYXVkIjoicmlzLnNob2hhbkBnbWFpbC5jb20ifQ.z_WySCFLDPZHQNJZ8juTGElQHeTnci-yG9RIRIT5XaA`,
        },
      });
    }

    return next.handle(request);
  }
}
