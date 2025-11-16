import { Route } from "@angular/router";
import { Page404Component } from "../authentication/page404/page404.component";
import { UserComponent } from "./users/user.component";
import { RoleComponent } from "./roles/role.component";
import { AssignRoleComponent } from "./assign-role/assign-role.component";
import { AssignClaimComponent } from "./assign-claim/assign-claim.component";

export const SECURITY_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'user',
    pathMatch: 'full',
  },
  {
    path: 'user',
    component: UserComponent,
  },
  {
    path: 'role',
    component: RoleComponent,
  },
  {
    path: 'assign-role',
    component: AssignRoleComponent,
  },
  {
    path: 'assign-claim',
    component: AssignClaimComponent,
  },
  { path: "**", component: Page404Component },
];

