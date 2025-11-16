import {
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment.development';

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn
): Observable<HttpEvent<any>> => {
  //   const cookieService = inject(CookieService);
  //  const token = cookieService.get('your-token');

  if (req.method === 'POST' && req.url.includes('api/login')) {
    const cloned = req.clone({
      setHeaders: {
        API_KEY: environment.api_key,
      },
    });
    return next(cloned);
  } else {
    const userString = localStorage.getItem('currentUser');
    const user = userString ? JSON.parse(userString) : null;
    if (user && user.token) {
      const cloned = req.clone({
        setHeaders: {
          authorization: `Bearer ${user.token}`,
        },
      });
      return next(cloned);
    } else {
      return next(req);
    }
  }
};
