import { Route } from '@angular/router';

export const reportRoutes: Route[] = [
  {
    path: 'stock',
    loadComponent: () =>
      import('./components/stock-report/stock-report.component').then(
        (m) => m.StockReportComponent
      ),
  },
  {
    path: 'daily-stock-book',
    loadComponent: () =>
      import('./components/daily-stock-book/daily-stock-book.component').then(
        (m) => m.DailyStockBookComponent
      ),
  },
  {
    path: 'cashbook',
    loadComponent: () =>
      import('./components/cashbook/cashbook.component').then(
        (m) => m.CashbookComponent
      ),
  },
  {
    path: 'ledger-book',
    loadComponent: () =>
      import('./components/ledger-book/ledger-book.component').then(
        (m) => m.LedgerBookComponent
      ),
  },
  {
    path: 'trial-balance',
    loadComponent: () =>
      import('./trial-balance/trial-balance.component').then(
        (m) => m.TrialBalanceComponent
      ),
  },
  {
    path: 'balance-sheet',
    loadComponent: () =>
      import('./balance-sheet/balance-sheet.component').then(
        (m) => m.BalanceSheetComponent
      ),
  },
];
