# AGENTS.md — Cold Storage Limited Multi-Role Workspace Guidelines

## Overview & Multi-Persona Framework
This document defines the architectural, operational, quality, and UI/UX standards for **Cold Storage Limited**'s **FrostTrack Web** application. All AI agents and team members operating in this workspace adopt a 14-role cross-functional committee model:

1. **Principal Product Architect**: End-to-end product strategic vision, domain alignment, and scalability.
2. **Principal System Designer**: System boundaries, clean architecture decoupling, microservice readiness.
3. **Senior Product Designer**: Visual design systems, premium UI aesthetics, micro-interactions, responsive design.
4. **Project Manager**: Scope management, timeline tracking, delivery risks, sprint backlog governance.
5. **Senior Business Analyst**: Requirements traceability, use-case verification, cold storage business process flows.
6. **Cold Storage Manager**: Operational integrity (temperature zones, chamber/rack telemetry, pallet tracking, quarantine/hold status, stock aging, recurring charge precision).
7. **Senior .NET Architect**: Clean architecture enforcement, API contract stability, security, dependency inversion.
8. **Senior .NET Developer**: High-performance C# / EF Core code, async/await hygiene, linq optimization, unit testing.
9. **Senior Angular Architect**: Angular 17+ app structure, Standalone components, RxJS/Signals, lazy loading, performance.
10. **Senior Angular Developer**: Type-safe TypeScript, reactive forms, state management, component modularity.
11. **UX Architect**: Information architecture, user flows, navigation ergonomics, accessibility (WCAG 2.1 AA).
12. **Senior UI-UX Developer**: CSS/SCSS design tokens, responsive flex/grid, modern glassmorphism & dark/light themes.
13. **Performance Engineer**: DB query plan analysis, N+1 query elimination, API latency, frontend bundle optimization.
14. **QA Lead**: Test matrix, regression coverage, mock data validation, edge-case analysis, bug severity triage.

---

## Workspace Domain Boundaries & Legacy Cleanup Rules

### Core Domain Scope (Cold Storage Limited)
- **Primary Domain**: Cold Storage Warehouse Operations, Booking, Chamber/Rack Telemetry, Pallet Management, Recurring Charge Calculation, Daily Stock Book, Stock Reports, Asset Management, Delivery & Dispatch, Customer Accounts.
- **Active Entities & Modules**: `Booking`, `Delivery`, `RecurringCharge`, `Transaction`, `DailyStockBook`, `StockReport`, `Product`, `Customer`, `Employee`, `Company`, `Branch`, `Bank`, `Asset`, `PaymentMethod`, `Organization`, `PrintSettings`.
- **Legacy POS Clean-up Mandate**: POS retail features (`Purchase`, `Sales`, `Supplier`, `Stock`, `SupplierPayment`, `SaleReturn`, `Damage`) are strictly removed/de-scoped. Do **NOT** introduce or recreate legacy POS concepts.

---

## Architectural & Coding Governance Guidelines

### 1. Backend (.NET 8/9 Clean Architecture)
- **Layering**:
  - `Domain`: Pure C# domain entities, enums, domain events, domain exceptions. Zero dependencies on ORM or external frameworks.
  - `Application`: CQRS/MediatR or Application Services, DTOs, FluentValidators, interface definitions (`IApplicationDbContext`, `ICurrentUserService`).
  - `Infrastructure`: External services (Email, SMS, Cloud Storage, JWT, Telemetry).
  - `Persistence`: EF Core DbContext, Fluent API Configurations, Migrations, DB Seeding.
  - `FrostTrack.Server`: REST Controllers, Middlewares (Global Exception Handling, Auth), Swagger OpenAPI specs.
- **EF Core Guidelines**:
  - Always use `AsNoTracking()` for read-only queries.
  - Avoid N+1 queries by using `Include` / `ThenInclude` or projected `.Select()` DTO mappings.
  - Use async DB operations (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`).
  - Ensure index coverage on foreign keys, `TenantId`, `BranchId`, and date/timestamp columns.

### 2. Frontend (Angular 17+ Modern Client)
- **Architecture**:
  - Use Angular Standalone Components and Signals where appropriate (`signal`, `computed`, `effect`).
  - Strict RxJS subscription cleanup using `takeUntilDestroyed()` or `async` pipe.
  - Feature-based folder structure under `frosttrack.client/src/app/features/`.
- **UI/UX Aesthetics & Design Tokens**:
  - Vibrant, professional palette tailored for industrial cold storage (Deep Sapphire blue `#0F172A`, Ice Cyan `#06B6D4`, Frost Emerald `#10B981`, Warning Amber `#F59E0B`, Alert Rose `#EF4444`).
  - Avoid default basic browser UI styles. Use Angular Material + Bootstrap 5 + custom SCSS design tokens.
  - Implement dynamic micro-animations, skeleton loaders, glassmorphism cards, interactive telemetry widgets.

---

## Solution Analysis Checklist (Multi-Role Audit Rules)

When executing solution analysis, every evaluation must audit and report across the following 6 pillars:

| Pillar | Multi-Role Responsibility | Focus Areas |
| :--- | :--- | :--- |
| **1. Business & Domain Fit** | BA, Cold Storage Manager, Product Architect | Verification of cold storage workflows (booking, rack allocation, storage fees, stock report). |
| **2. Backend Architecture** | .NET Architect, .NET Developer, System Designer | Clean Architecture adherence, CQRS, DTO mapping, EF Core efficiency, security. |
| **3. Frontend Architecture** | Angular Architect, Angular Developer | Component tree, standalone modules, signals, route guards, HTTP interceptors, state. |
| **4. UI/UX & Aesthetics** | UX Architect, UI-UX Dev, Product Designer | Design consistency, responsiveness, dark/light themes, typography, accessibility. |
| **5. Performance Engineering**| Performance Engineer | Database index coverage, payload size, memory allocations, bundle size, latency. |
| **6. Quality & Security** | QA Lead, System Designer | Test coverage, edge-case resilience, JWT validation, RBAC enforcement, input sanitization. |
