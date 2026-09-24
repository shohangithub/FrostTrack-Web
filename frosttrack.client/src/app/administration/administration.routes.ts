import { Route } from "@angular/router";
import { Page404Component } from "../authentication/page404/page404.component";
import { ProductComponent } from "./components/product/product.component";
import { SeasonArchiveComponent } from "./components/season-archive/season-archive.component";

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
  {
    path: 'season-archive',
    component: SeasonArchiveComponent,
  },
  { path: "**", component: Page404Component },
];

