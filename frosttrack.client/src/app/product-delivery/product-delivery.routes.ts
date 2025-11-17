import { Route } from '@angular/router';
import { ProductDeliveryComponent } from './components/product-delivery/product-delivery.component';
import { ProductDeliveryListComponent } from './components/product-delivery-list/product-delivery-list.component';
import { ProductDeliveryRecordComponent } from './components/product-delivery-record/product-delivery-record.component';

export const PRODUCT_DELIVERY_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    component: ProductDeliveryListComponent,
    data: { title: 'Product Delivery List' },
  },
  {
    path: 'add',
    component: ProductDeliveryComponent,
    data: { title: 'Add Product Delivery' },
  },
  {
    path: 'edit/:id',
    component: ProductDeliveryComponent,
    data: { title: 'Edit Product Delivery' },
  },
  {
    path: 'record/:id',
    component: ProductDeliveryRecordComponent,
    data: { title: 'Product Delivery Record' },
  },
];
