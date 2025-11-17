import { Route } from '@angular/router';
import { ProductReceiveComponent } from './components/product-receive/product-receive.component';
import { ProductReceiveListComponent } from './components/product-receive-list/product-receive-list.component';
import { ProductReceiveRecordComponent } from './components/product-receive-record/product-receive-record.component';

export const PRODUCT_RECEIVE_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    component: ProductReceiveListComponent,
    data: { title: 'Product Receive List' },
  },
  {
    path: 'add',
    component: ProductReceiveComponent,
    data: { title: 'Add Product Receive' },
  },
  {
    path: 'edit/:id',
    component: ProductReceiveComponent,
    data: { title: 'Edit Product Receive' },
  },
  {
    path: 'record/:id',
    component: ProductReceiveRecordComponent,
    data: { title: 'Product Receive Record' },
  },
];
