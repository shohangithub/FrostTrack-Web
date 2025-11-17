import { Route } from '@angular/router';
import { MainLayoutComponent } from './layout/app-layout/main-layout/main-layout.component';
import { AuthGuard } from './core/guard/auth.guard';
import { AuthLayoutComponent } from './layout/app-layout/auth-layout/auth-layout.component';
import { Page404Component } from './authentication/page404/page404.component';

export const APP_ROUTE: Route[] = [
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: '/authentication/signin', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTE),
      },
      {
        path: 'security',
        loadChildren: () =>
          import('./security/security.routes').then((m) => m.SECURITY_ROUTE),
      },
      {
        path: 'common',
        loadChildren: () =>
          import('./common/common.routes').then((m) => m.COMMON_ROUTE),
      },
      {
        path: 'administration',
        loadChildren: () =>
          import('./administration/administration.routes').then(
            (m) => m.ADMINISTRATION_ROUTE
          ),
      },
      {
        path: 'purchase',
        loadChildren: () =>
          import('./purchase/purchase.routes').then((m) => m.PURCHASE_ROUTE),
      },
      {
        path: 'product-receive',
        loadChildren: () =>
          import('./product-receive/product-receive.routes').then(
            (m) => m.PRODUCT_RECEIVE_ROUTE
          ),
      },
      {
        path: 'supplier-payment',
        loadChildren: () =>
          import('./supplier-payment/supplier-payment.routes').then(
            (m) => m.SUPPLIER_PAYMENT_ROUTE
          ),
      },
      {
        path: 'sales',
        loadChildren: () =>
          import('./sales/sales.routes').then((m) => m.SALES_ROUTE),
      },
      {
        path: 'advance-table',
        loadChildren: () =>
          import('./advance-table/advance-table.routes').then(
            (m) => m.ADVANCE_TABLE_ROUTE
          ),
      },
      {
        path: 'extra-pages',
        loadChildren: () =>
          import('./extra-pages/extra-pages.routes').then(
            (m) => m.EXTRA_PAGES_ROUTE
          ),
      },
      {
        path: 'multilevel',
        loadChildren: () =>
          import('./multilevel/multilevel.routes').then(
            (m) => m.MULTILEVEL_ROUTE
          ),
      },
    ],
  },
  {
    path: 'authentication',
    component: AuthLayoutComponent,
    loadChildren: () =>
      import('./authentication/auth.routes').then((m) => m.AUTH_ROUTE),
  },
  { path: '**', component: Page404Component },
];
