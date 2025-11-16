import { Route } from '@angular/router';
import { Page404Component } from '../authentication/page404/page404.component';
import { PurchaseComponent } from './components/purchase/purchase.component';
import { PurchaseRecordComponent } from './components/purchase-record/purchase-record.component';
import { PurchaseInvoiceListComponent } from './components/purchase-invoice-list/purchase-invoice-list.component';
import { PurchaseInvoicePrintComponent } from './components/purchase-invoice-print/purchase-invoice-print.component';

export const PURCHASE_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'create',
    pathMatch: 'full',
  },
  {
    path: 'create',
    component: PurchaseComponent,
  },
  {
    path: 'edit/:id',
    component: PurchaseComponent,
  },
  {
    path: 'invoices',
    component: PurchaseInvoiceListComponent,
  },
  {
    path: 'record',
    component: PurchaseRecordComponent,
  },
  {
    path: 'invoice-print',
    component: PurchaseInvoicePrintComponent,
  },
  { path: '**', component: Page404Component },
];
