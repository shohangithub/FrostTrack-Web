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
    path: 'delivery-based',
    loadComponent: () =>
      import(
        './components/delivery-bill-collection/delivery-bill-collection.component'
      ).then((m) => m.DeliveryBillCollectionComponent),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./components/bill-collection/bill-collection.component').then(
        (m) => m.BillCollectionComponent
      ),
  },
  {
    path: 'receipt-print',
    loadComponent: () =>
      import(
        './components/bill-collection-receipt-print/bill-collection-receipt-print.component'
      ).then((m) => m.BillCollectionReceiptPrintComponent),
  },
  {
    path: 'receipt-print/:id/:backurl',
    loadComponent: () =>
      import(
        './components/bill-collection-receipt-print/bill-collection-receipt-print.component'
      ).then((m) => m.BillCollectionReceiptPrintComponent),
  },
];
