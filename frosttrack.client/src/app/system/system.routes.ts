import { Route } from '@angular/router';

export const SYSTEM_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'company',
    pathMatch: 'full',
  },
  {
    path: 'company',
    loadComponent: () =>
      import('./components/company-list/company-list.component').then(
        (c) => c.CompanyListComponent
      ),
  },
];
