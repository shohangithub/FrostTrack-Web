import { Routes } from '@angular/router';

export const salaryPaymentRoutes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    loadComponent: () =>
      import(
        './components/salary-payment-list/salary-payment-list.component'
      ).then((m) => m.SalaryPaymentListComponent),
  },
  {
    path: 'add',
    loadComponent: () =>
      import(
        './components/salary-payment-form/salary-payment-form.component'
      ).then((m) => m.SalaryPaymentFormComponent),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import(
        './components/salary-payment-form/salary-payment-form.component'
      ).then((m) => m.SalaryPaymentFormComponent),
  },
  {
    path: 'view/:id',
    loadComponent: () =>
      import(
        './components/salary-payment-form/salary-payment-form.component'
      ).then((m) => m.SalaryPaymentFormComponent),
  },
  {
    path: 'receipt-print',
    loadComponent: () =>
      import(
        './components/salary-receipt-print/salary-receipt-print.component'
      ).then((m) => m.SalaryReceiptPrintComponent),
  },
  {
    path: 'receipt-print/:id/:backurl',
    loadComponent: () =>
      import(
        './components/salary-receipt-print/salary-receipt-print.component'
      ).then((m) => m.SalaryReceiptPrintComponent),
  },
];
