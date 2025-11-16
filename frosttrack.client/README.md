# FrostTrack Client - Angular Frontend

The frontend application for the FrostTrack Point of Sale system, built with Angular 17.

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 17.0.8.

## Prerequisites

- Node.js v18 or higher
- npm v9 or higher
- Angular CLI 17

## Installation

```bash
npm install
```

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

To run with proxy configuration (to connect to the backend API):

```bash
npm start
```

This will use the proxy configuration defined in `src/proxy.conf.js` to forward API requests to `https://localhost:7101`.

## Application Structure

```
src/
├── app/
│   ├── administration/      # Product management module
│   ├── authentication/      # Login, signup, password reset
│   ├── common/             # Common settings (categories, units, customers, suppliers)
│   ├── dashboard/          # Dashboard and analytics
│   ├── layout/             # Application layout components
│   │   ├── header/         # Top navigation bar
│   │   ├── sidebar/        # Side navigation menu
│   │   ├── footer/         # Footer component
│   │   └── app-layout/     # Layout wrappers
│   ├── purchase/           # Purchase management module
│   ├── sales/              # Sales management module
│   ├── security/           # User management module
│   ├── shared/             # Shared components and services
│   ├── core/               # Core services and guards
│   ├── config/             # Application configuration
│   └── utils/              # Utility functions
├── assets/                 # Static assets (images, fonts, etc.)
└── environments/           # Environment configurations
```

## Key Features

### Modules

- **Dashboard**: Analytics and business insights
- **Sales**: Sales order creation, invoicing, and records
- **Purchase**: Purchase order management and supplier tracking
- **Products**: Product catalog, barcode generation, pricing
- **Inventory**: Stock management and tracking
- **Customers & Suppliers**: Contact management
- **Users**: User management and authentication

### UI Components

- Responsive Material Design interface
- Advanced data tables with sorting, filtering, and pagination
- Interactive charts and graphs
- Toast notifications
- Print functionality for invoices
- Barcode generation and printing
- Rich text editor for descriptions
- Date/time pickers
- Color pickers
- Masked inputs

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use:

```bash
ng generate directive|pipe|service|class|guard|interface|enum|module
```

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

For production build:

```bash
ng build --configuration production
```

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

```bash
npm test
```

## Linting

Run ESLint to check code quality:

```bash
npm run lint
```

## Environment Configuration

The application uses environment files for configuration:

- `src/environments/environment.ts` - Development environment
- `src/environments/environment.prod.ts` - Production environment

### Proxy Configuration

The proxy configuration in `src/proxy.conf.js` routes API requests to the backend server:

```javascript
{
  context: ["/weatherforecast"],
  target: "https://localhost:7101",
  secure: false
}
```

Add additional API endpoints to this configuration as needed.

## Technology Stack

### Core Framework

- Angular 17.0.4
- TypeScript 5.2.2
- RxJS 7.8.1

### UI Libraries

- Angular Material 17.0.1
- Bootstrap 5.3.2
- ng-bootstrap 16.0.0
- Angular CDK 17.0.1

### Charts & Visualizations

- ApexCharts 3.44.0
- ng-apexcharts 1.8.0
- ngx-charts 20.5.0
- ng2-charts 5.0.3
- ngx-echarts 17.1.0

### Utilities

- ngx-toastr 18.0.0 (Toast notifications)
- SweetAlert2 11.10.1 (Alerts)
- ngx-datatable 20.1.0 (Advanced tables)
- ngx-mask 17.0.1 (Input masking)
- ngx-print 1.5.1 (Print functionality)
- ngx-barcode 1.0.1 (Barcode generation)
- ngx-color-picker 16.0.0 (Color picker)
- @ckeditor/ckeditor5-angular 7.0.1 (Rich text editor)
- moment 2.29.4 (Date handling)
- xlsx 0.18.5 (Excel export)

### State Management & Routing

- Angular Router 17.0.4
- angular-jwt-updated 17.0.1 (JWT handling)

### Development Tools

- Angular CLI 17.0.3
- ESLint 8.54.0
- Karma & Jasmine (Testing)

## Application Routes

### Public Routes

- `/authentication/signin` - Login page
- `/authentication/signup` - Registration page
- `/authentication/forgot` - Forgot password
- `/authentication/reset` - Reset password
- `/authentication/page404` - 404 error page
- `/authentication/page500` - 500 error page

### Protected Routes (Require Authentication)

- `/dashboard/main` - Main dashboard
- `/dashboard/dashboard2` - Alternative dashboard

**Security**

- `/security/user` - User management

**Common Settings**

- `/common/product-category` - Product categories
- `/common/base-unit` - Base units of measurement
- `/common/unit-conversion` - Unit conversions
- `/common/customer` - Customer management
- `/common/supplier` - Supplier management

**Administration**

- `/administration/product` - Product management

**Purchase**

- `/purchase/create` - Create purchase order
- `/purchase/edit/:id` - Edit purchase order
- `/purchase/record` - Purchase records
- `/purchase/invoices` - Purchase invoice list
- `/purchase/invoice-print/:id` - Print purchase invoice

**Sales**

- `/sales/create` - Create sales order
- `/sales/edit/:id` - Edit sales order
- `/sales/record` - Sales records
- `/sales/invoices` - Sales invoice list
- `/sales/invoice-print/:id` - Print sales invoice

## Authentication

The application uses JWT-based authentication with the following flow:

1. User logs in via `/authentication/signin`
2. Backend returns JWT token
3. Token is stored in browser storage
4. AuthGuard protects routes requiring authentication
5. Token is sent with all API requests in the Authorization header

## API Integration

The application communicates with the backend API through services located in each module's `services` folder. All API calls are proxied through the configuration in `proxy.conf.js`.

### Example Service Usage

```typescript
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

@Injectable({
  providedIn: "root",
})
export class ProductService {
  private apiUrl = "/api/product";

  constructor(private http: HttpClient) {}

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl);
  }
}
```

## Internationalization (i18n)

The application supports multiple languages using ngx-translate. Translation files are located in `src/assets/i18n/`.

## Styling

The application uses a combination of:

- Bootstrap 5 for layout and grid system
- Angular Material for UI components
- Custom SCSS for theming
- Global styles in `src/styles.scss`

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.io/cli) page.

For project-specific questions, refer to the main [README](../README.md) in the root directory.
