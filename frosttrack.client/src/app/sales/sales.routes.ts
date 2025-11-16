import { Route } from '@angular/router';
import { Page404Component } from '../authentication/page404/page404.component';
import { SalesComponent } from './components/sales/sales.component';
import { SalesInvoiceListComponent } from './components/sales-invoice-list/sales-invoice-list.component';
import { SalesInvoicePrintComponent } from './components/sales-invoice-print/sales-invoice-print.component';
import { SalesRecordComponent } from './components/sales-record/sales-record.component';
import { SaleReturnComponent } from './components/sale-return/sale-return.component';
import { SaleReturnRecordComponent } from './components/sale-return-record/sale-return-record.component';

export const SALES_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'create',
    pathMatch: 'full',
  },
  {
    path: 'create',
    component: SalesComponent,
  },
  {
    path: 'edit/:id',
    component: SalesComponent,
  },
  {
    path: 'invoices',
    component: SalesInvoiceListComponent,
  },
  {
    path: 'record',
    component: SalesRecordComponent,
  },
  {
    path: 'invoice-print/:id',
    component: SalesInvoicePrintComponent,
  },
  {
    path: 'sale-return/create',
    component: SaleReturnComponent,
  },
  {
    path: 'sale-return/edit/:id',
    component: SaleReturnComponent,
  },
  {
    path: 'sale-return/record',
    component: SaleReturnRecordComponent,
  },
  { path: '**', component: Page404Component },
];
