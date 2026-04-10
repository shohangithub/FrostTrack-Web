import { Route } from '@angular/router';

export const reportRoutes: Route[] = [
  {
    path: 'stock',
    loadComponent: () =>
      import('./components/stock-report/stock-report.component').then(
        (m) => m.StockReportComponent,
      ),
  },
  {
    path: 'daily-stock-book',
    loadComponent: () =>
      import('./components/daily-stock-book/daily-stock-book.component').then(
        (m) => m.DailyStockBookComponent,
      ),
  },
  {
    path: 'datewise-booking-report',
    loadComponent: () =>
      import('./components/datewise-booking-report/datewise-booking-report.component').then(
        (m) => m.DatewiseBookingReportComponent,
      ),
  },
  {
    path: 'datewise-delivery-report',
    loadComponent: () =>
      import('./components/datewise-delivery-report/datewise-delivery-report.component').then(
        (m) => m.DatewiseDeliveryReportComponent,
      ),
  },
  {
    path: 'monthly-salary-report',
    loadComponent: () =>
      import('./components/monthly-salary-report/monthly-salary-report.component').then(
        (m) => m.MonthlySalaryReportComponent,
      ),
  },
  {
    path: 'cashbook',
    loadComponent: () =>
      import('./components/cashbook/cashbook.component').then(
        (m) => m.CashbookComponent,
      ),
  },
  // {
  //   path: 'ledger-book',
  //   loadComponent: () =>
  //     import('./components/ledger-book/ledger-book.component').then(
  //       (m) => m.LedgerBookComponent
  //     ),
  // },
  {
    path: 'bankbook',
    loadComponent: () =>
      import('./components/bankbook/bankbook.component').then(
        (m) => m.BankbookComponent,
      ),
  },
  {
    path: 'general-ledger',
    loadComponent: () =>
      import('./components/general-ledger/general-ledger.component').then(
        (m) => m.GeneralLedgerComponent,
      ),
  },
  {
    path: 'trial-balance',
    loadComponent: () =>
      import('./trial-balance/trial-balance.component').then(
        (m) => m.TrialBalanceComponent,
      ),
  },
  {
    path: 'balance-sheet',
    loadComponent: () =>
      import('./balance-sheet/balance-sheet.component').then(
        (m) => m.BalanceSheetComponent,
      ),
  },
  {
    path: 'employee-report',
    loadComponent: () =>
      import('./components/employee-report/employee-report.component').then(
        (m) => m.EmployeeReportComponent,
      ),
  },
];
