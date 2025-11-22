import { Route } from '@angular/router';

export const billCollectionRoutes: Route[] = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    loadComponent: () =>
      import(
        './components/bill-collection-list/bill-collection-list.component'
      ).then((m) => m.BillCollectionListComponent),
  },
  {
    path: 'add',
    loadComponent: () =>
      import('./components/bill-collection/bill-collection.component').then(
        (m) => m.BillCollectionComponent
      ),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./components/bill-collection/bill-collection.component').then(
        (m) => m.BillCollectionComponent
      ),
  },
];
