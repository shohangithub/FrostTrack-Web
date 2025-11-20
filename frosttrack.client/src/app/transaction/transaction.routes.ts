import { Route } from '@angular/router';

export const transactionRoutes: Route[] = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    loadComponent: () =>
      import('./components/transaction-list/transaction-list.component').then(
        (m) => m.TransactionListComponent
      ),
  },
];
