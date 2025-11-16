# FrostTrack - Point of Sale Web Application

A comprehensive Point of Sale (POS) web application built with Angular 17 and .NET Core, designed for retail and inventory management.

## 🚀 Features

### 📊 Dashboard & Analytics

- **Main Dashboard**: Real-time business insights and analytics
- **Dashboard 2**: Alternative dashboard view with additional metrics
- Visual charts and graphs for sales, purchases, and inventory tracking

### 🔐 Authentication & Security

- **User Management**: Complete user administration system
- **Sign In/Sign Up**: Secure authentication with JWT tokens
- **Password Management**: Forgot password and reset password functionality
- **Role-Based Access Control**: Manage user permissions and roles
- **Auth Guard**: Protected routes with authentication middleware

### 🏪 Sales Management

- **Sales Order Creation**: Create and manage sales orders
- **Invoice Generation**: Automatic invoice generation with unique invoice numbers
- **Sales Records**: Comprehensive sales transaction history
- **Invoice Printing**: Professional invoice printing functionality
- **Customer Management**: Track customer information and purchase history
- **Multiple Sales Types**: Support for different sales types (retail, wholesale)
- **VAT/Tax Calculation**: Automatic VAT and discount calculations
- **Payment Tracking**: Track paid amounts and outstanding balances

### 📦 Purchase Management

- **Purchase Order Creation**: Create and manage purchase orders
- **Supplier Management**: Maintain supplier information and relationships
- **Purchase Records**: Complete purchase transaction history
- **Invoice Management**: Purchase invoice listing and tracking
- **Invoice Printing**: Print purchase invoices
- **Stock Updates**: Automatic stock updates on purchase
- **Cost Tracking**: Track purchase costs, VAT, and discounts

### 🏷️ Product Management

- **Product Catalog**: Comprehensive product management
- **Product Categories**: Organize products by categories
- **Barcode Generation**: Generate custom and standard barcodes
- **Product Images**: Upload and manage product images
- **Pricing Management**:
  - Purchase rate
  - Selling rate
  - Wholesale price
- **Product Types**:
  - Finished goods
  - Raw materials
  - Services
- **Stock Management**: Real-time inventory tracking
- **Reorder Levels**: Set minimum stock levels for automatic alerts
- **Unit Conversions**: Flexible unit management system

### 📐 Common Settings

- **Product Categories**: Create and manage product categories
- **Base Units**: Define base units of measurement (kg, pcs, ltr, etc.)
- **Unit Conversions**: Set up conversion rates between units
- **Customer Management**: Maintain customer database
- **Supplier Management**: Manage supplier information
- **Branch Management**: Multi-branch support

### 📈 Inventory Management

- **Stock Tracking**: Real-time inventory levels
- **Stock In/Out**: Track stock movements
- **Purchase Details**: Detailed purchase transaction records
- **Sales Details**: Detailed sales transaction records
- **Reorder Alerts**: Notifications for low stock items

### 💼 Administration

- **Product Administration**: Advanced product management
- **Multi-Branch Support**: Manage multiple store locations
- **Organization Settings**: Configure company and branch details
- **User Administration**: Manage system users and permissions

### 📄 Reporting & Printing

- **Sales Reports**: Comprehensive sales analytics
- **Purchase Reports**: Purchase transaction reports
- **Invoice Printing**: Professional invoice templates
- **Barcode Printing**: Print product barcodes

### 🎨 User Interface

- **Responsive Design**: Mobile-friendly interface
- **Modern UI**: Built with Angular Material and Bootstrap
- **Dark/Light Mode**: Theme customization
- **Multi-language Support**: i18n support via ngx-translate
- **Loading Indicators**: Progress bars and loading states
- **Toast Notifications**: User-friendly alerts and messages
- **Advanced Tables**: Sortable, filterable data tables with ngx-datatable

## 🛠️ Technology Stack

### Frontend

- **Angular 17**: Modern web framework
- **TypeScript 5.2**: Type-safe programming
- **Angular Material**: UI component library
- **Bootstrap 5**: Responsive CSS framework
- **RxJS**: Reactive programming
- **NgRx/Signals**: State management
- **Angular Router**: Navigation and routing
- **JWT Authentication**: Secure token-based auth

### UI Components & Libraries

- **ng-bootstrap**: Bootstrap components for Angular
- **ngx-toastr**: Toast notifications
- **SweetAlert2**: Beautiful alert dialogs
- **ApexCharts**: Interactive charts
- **ng2-charts**: Chart.js wrapper
- **ngx-echarts**: ECharts integration
- **FullCalendar**: Calendar and scheduling
- **ngx-datatable**: Advanced data tables
- **ngx-mask**: Input masking
- **ngx-color-picker**: Color picker component
- **CKEditor**: Rich text editor
- **ngx-barcode**: Barcode generation
- **ngx-print**: Print functionality

### Backend

- **.NET Core**: Backend framework
- **Entity Framework Core**: ORM for database operations
- **ASP.NET Core Web API**: RESTful API
- **SQL Server**: Database
- **JWT Authentication**: Token-based authentication
- **Clean Architecture**: Organized codebase with separation of concerns

### Architecture Layers

- **Domain**: Entity models and business logic
- **Application**: Application services and DTOs
- **Infrastructure**: External services and implementations
- **Persistence**: Database context and migrations
- **Presentation**: API controllers and endpoints

## 📋 Prerequisites

- Node.js (v18 or higher)
- npm (v9 or higher)
- .NET 8.0 SDK
- SQL Server 2019 or higher
- Angular CLI 17

## 🔧 Installation & Setup

### Frontend Setup

```bash
cd frosttrack.client
npm install
npm start
```

The application will run on `http://localhost:4200/`

### Backend Setup

```bash
cd FrostTrack.Server
dotnet restore
dotnet run
```

The API will run on `https://localhost:7101`

### Database Setup

1. Update the connection string in `appsettings.json`
2. Run migrations:

```bash
dotnet ef database update
```

## 🧪 Testing

### Frontend Tests

```bash
cd frosttrack.client
npm test
```

### Linting

```bash
npm run lint
```

### Build

```bash
npm run build
```

## 📁 Project Structure

```
FrostTrack/
├── frosttrack.client/              # Angular frontend application
│   ├── src/
│   │   ├── app/
│   │   │   ├── administration/ # Product management
│   │   │   ├── authentication/ # Login, signup, etc.
│   │   │   ├── common/         # Common settings
│   │   │   ├── dashboard/      # Dashboard components
│   │   │   ├── layout/         # Layout components
│   │   │   ├── purchase/       # Purchase management
│   │   │   ├── sales/          # Sales management
│   │   │   ├── security/       # User management
│   │   │   └── shared/         # Shared components
│   │   └── assets/             # Static assets
│   └── package.json
├── FrostTrack.Server/              # .NET Core backend
│   ├── Controllers/            # API controllers
│   ├── Middlewares/            # Custom middleware
│   └── Program.cs
├── Domain/                     # Domain entities
│   ├── Entitites/
│   └── Enums/
├── Application/                # Application layer
│   ├── Contractors/
│   └── Services/
├── Infrastructure/             # Infrastructure layer
│   ├── Services/
│   └── Validators/
└── Persistence/                # Data access layer
    └── Contexts/
```

## 🔑 Key Modules

### Sales Module

- Create sales orders
- Generate invoices
- Track customer payments
- Print sales invoices
- View sales history and records

### Purchase Module

- Create purchase orders
- Manage suppliers
- Track purchase costs
- Update inventory automatically
- Print purchase invoices

### Product Module

- Add/edit products
- Generate barcodes
- Upload product images
- Manage pricing (purchase, retail, wholesale)
- Set reorder levels
- Track stock levels

### User Management

- Create and manage users
- Assign roles and permissions
- Secure authentication
- Password management

## 🌐 API Endpoints

The backend provides RESTful APIs for:

- `/api/sales` - Sales operations
- `/api/purchase` - Purchase operations
- `/api/product` - Product management
- `/api/customer` - Customer management
- `/api/supplier` - Supplier management
- `/api/users` - User management
- `/api/login` - Authentication
- `/api/productcategory` - Category management
- `/api/baseunit` - Unit management
- `/api/unitconversion` - Unit conversion
- `/api/branch` - Branch management

## 📱 Screenshots

_(Screenshots can be added here to showcase the application)_

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License.

## 👥 Authors

- Shohan - Initial work

## 🙏 Acknowledgments

- Angular team for the amazing framework
- .NET Core team for the robust backend framework
- All open-source library contributors
