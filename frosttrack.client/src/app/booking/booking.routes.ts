import { Route } from '@angular/router';

export const BOOKING_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    loadComponent: () =>
      import('./components/booking-list/booking-list.component').then(
        (m) => m.BookingListComponent
      ),
  },
  {
    path: 'add',
    loadComponent: () =>
      import('./components/booking/booking.component').then(
        (m) => m.BookingComponent
      ),
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./components/booking/booking.component').then(
        (m) => m.BookingComponent
      ),
  },
  {
    path: 'record/:id',
    loadComponent: () =>
      import('./components/booking-record/booking-record.component').then(
        (m) => m.BookingRecordComponent
      ),
  },
  {
    path: 'invoice-print',
    loadComponent: () =>
      import(
        './components/booking-invoice-print/booking-invoice-print.component'
      ).then((m) => m.BookingInvoicePrintComponent),
  },
  {
    path: 'invoice-print/:id/:backurl',
    loadComponent: () =>
      import(
        './components/booking-invoice-print/booking-invoice-print.component'
      ).then((m) => m.BookingInvoicePrintComponent),
  },
  {
    path: 'invoice-with-delivery-print',
    loadComponent: () =>
      import(
        './components/booking-invoice-with-delivery-print/booking-invoice-with-delivery-print.component'
      ).then((m) => m.BookingInvoiceWithDeliveryPrintComponent),
  },
];
