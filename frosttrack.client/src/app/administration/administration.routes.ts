import { Route } from "@angular/router";
import { Page404Component } from "../authentication/page404/page404.component";
import { ProductComponent } from "./components/product/product.component";

export const ADMINISTRATION_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'product',
    pathMatch: 'full',
  },
  {
    path: 'product',
    component: ProductComponent,
  },
  { path: "**", component: Page404Component },
];

