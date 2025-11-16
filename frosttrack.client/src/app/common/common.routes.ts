import { Route } from '@angular/router';
import { Page404Component } from '../authentication/page404/page404.component';
import { ProductCategoryComponent } from './components/product-category/product-category.component';
import { BaseUnitComponent } from './components/base-unit/base-unit.component';
import { UnitConversionComponent } from './components/unit-conversion/unit-conversion.component';
import { SupplierComponent } from './components/supplier/supplier.component';
import { CustomerComponent } from './components/customer/customer.component';
import { BranchComponent } from './components/branch/branch.component';
import { BankComponent } from './components/bank/bank.component';
import { BankTransactionComponent } from './components/bank-transaction/bank-transaction.component';
import { DamageComponent } from './components/damage/damage.component';
import { AssetComponent } from './components/asset/asset.component';
import { EmployeeComponent } from './components/employee/employee.component';
import { PaymentMethodComponent } from './components/payment-method/payment-method.component';

export const COMMON_ROUTE: Route[] = [
  {
    path: '',
    redirectTo: 'unit-conversion',
    pathMatch: 'full',
  },
  // {
  //   path: 'product-category',
  //   component: ProductCategoryComponent,
  // },
  {
    path: 'base-unit',
    component: BaseUnitComponent,
  },
  {
    path: 'unit-conversion',
    component: UnitConversionComponent,
  },
  {
    path: 'customer',
    component: CustomerComponent,
  },
  // {
  //   path: 'branch',
  //   component: BranchComponent,
  // },
  // {
  //   path: 'supplier',
  //   component: SupplierComponent,
  // },
  // {
  //   path: 'bank',
  //   component: BankComponent,
  // },
  // {
  //   path: 'bank-transaction',
  //   component: BankTransactionComponent,
  // },
  // {
  //   path: 'payment-method',
  //   component: PaymentMethodComponent,
  // },
  // {
  //   path: 'damage',
  //   component: DamageComponent,
  // },
  // {
  //   path: 'asset',
  //   component: AssetComponent,
  // },
  {
    path: 'employee',
    component: EmployeeComponent,
  },
  { path: '**', component: Page404Component },
];
