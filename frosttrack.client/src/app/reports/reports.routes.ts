import { Route } from '@angular/router';

export const reportRoutes: Route[] = [
  {
    path: 'stock',
    loadComponent: () =>
      import('./components/stock-report/stock-report.component').then(
        (m) => m.StockReportComponent
      ),
  },
];
