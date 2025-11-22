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
  {
    path: 'add',
    loadComponent: () =>
      import('./components/transaction/transaction.component').then(
        (m) => m.TransactionComponent
      ),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./components/transaction/transaction.component').then(
        (m) => m.TransactionComponent
      ),
  },
  {
    path: 'receipt-print',
    loadComponent: () =>
      import(
        './components/transaction-receipt-print/transaction-receipt-print.component'
      ).then((m) => m.TransactionReceiptPrintComponent),
  },
  {
    path: 'receipt-print/:id/:backurl',
    loadComponent: () =>
      import(
        './components/transaction-receipt-print/transaction-receipt-print.component'
      ).then((m) => m.TransactionReceiptPrintComponent),
  },
  {
    path: 'report',
    loadComponent: () =>
      import(
        './components/transaction-report/transaction-report.component'
      ).then((m) => m.TransactionReportComponent),
  },
];
