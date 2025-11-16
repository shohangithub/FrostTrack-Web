import { Route } from '@angular/router';
import { Page404Component } from '../authentication/page404/page404.component';
import { SupplierPaymentComponent } from './components/supplier-payment/supplier-payment.component';
import { SupplierPaymentRecordComponent } from './components/supplier-payment-record/supplier-payment-record.component';
import { SupplierPaymentListComponent } from './components/supplier-payment-list/supplier-payment-list.component';

export const SUPPLIER_PAYMENT_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'create',
    pathMatch: 'full',
  },
  {
    path: 'create',
    component: SupplierPaymentComponent,
  },
  {
    path: 'edit/:id',
    component: SupplierPaymentComponent,
  },
  {
    path: 'list',
    component: SupplierPaymentListComponent,
  },
  {
    path: 'record',
    component: SupplierPaymentRecordComponent,
  },
  { path: '**', component: Page404Component },
];
