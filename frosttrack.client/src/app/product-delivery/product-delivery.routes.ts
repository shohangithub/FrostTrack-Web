import { Route } from '@angular/router';
import { DeliveryComponent } from './components/product-delivery/product-delivery.component';
import { DeliveryListComponent } from './components/product-delivery-list/product-delivery-list.component';
import { DeliveryRecordComponent } from './components/product-delivery-record/product-delivery-record.component';
import { DeliveryInvoicePrintComponent } from './components/delivery-invoice-print/delivery-invoice-print.component';
import { DeliveryChallanComponent } from './components/delivery-challan/delivery-challan.component';
import { DeliveryChallanListComponent } from './components/delivery-challan-list/delivery-challan-list.component';
import { DeliveryChallanPrintComponent } from './components/delivery-challan-print/delivery-challan-print.component';

export const PRODUCT_DELIVERY_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    component: DeliveryListComponent,
    data: { title: 'Product Delivery List' },
  },
  {
    path: 'add',
    component: DeliveryComponent,
    data: { title: 'Add Product Delivery' },
  },
  {
    path: 'edit/:id',
    component: DeliveryComponent,
    data: { title: 'Edit Product Delivery' },
  },
  {
    path: 'record/:id',
    component: DeliveryRecordComponent,
    data: { title: 'Product Delivery Record' },
  },
  {
    path: 'invoice-print',
    component: DeliveryInvoicePrintComponent,
    data: { title: 'Delivery Invoice Print' },
  },
  {
    path: 'invoice-print/:id',
    component: DeliveryInvoicePrintComponent,
    data: { title: 'Delivery Invoice Print' },
  },
  {
    path: 'challan/list',
    component: DeliveryChallanListComponent,
    data: { title: 'Delivery Challan List' },
  },
  {
    path: 'challan/add',
    component: DeliveryChallanComponent,
    data: { title: 'Create Delivery Challan' },
  },
  {
    path: 'challan/edit/:id',
    component: DeliveryChallanComponent,
    data: { title: 'Edit Delivery Challan' },
  },
  {
    path: 'challan/print/:id',
    component: DeliveryChallanPrintComponent,
    data: { title: 'Print Delivery Challan' },
  },
];
