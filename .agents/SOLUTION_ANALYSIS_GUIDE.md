# 📖 Master Instructions: Solution Analysis Guide for FrostTrack Web

## Executive Purpose
This document provides complete instructions for executing an end-to-end multi-perspective analysis of the **FrostTrack Web** solution for **Cold Storage Limited**. It operationalizes the 14-role expert committee (Product Architect, System Designer, Product Designer, PM, BA, Cold Storage Manager, .NET Architect, .NET Dev, Angular Architect, Angular Dev, UX Architect, UI-UX Dev, Performance Engineer, QA Lead).

---

## 🎭 14-Role Responsibilities & Decision Framework

| Role | Key Focus & Responsibility | Key Inspection Scope |
| :--- | :--- | :--- |
| **Principal Product Architect** | Overall solution vision, modular design, tech stack longevity | Solution boundaries, layer coupling, architecture standards |
| **Principal System Designer** | Scalability, multi-tenancy, cross-cutting concerns, integration | Clean Architecture patterns, database design, API design |
| **Senior Product Designer** | Product visual aesthetics, layout harmony, component design | UI visual quality, typography, color harmony, themes |
| **Project Manager** | Scope control, deliverable risks, priority alignment | Feature completeness, legacy POS removal validation |
| **Senior Business Analyst** | Requirements mapping, use case validity, workflow integrity | Business rules, entity relationships, validation rules |
| **Cold Storage Manager** | Operations integrity: storage, booking, telemetry, billing | Booking flows, daily stock book, recurring storage charges |
| **Senior .NET Architect** | .NET 8/9 C# architecture, dependency inversion, security | Clean Architecture, DTO mapping, auth, exception middleware |
| **Senior .NET Developer** | C# code quality, EF Core queries, async/await hygiene | LINQ performance, `AsNoTracking()`, null safety |
| **Senior Angular Architect** | Angular 17 structure, state management, routing, compilation | Standalone components, RxJS/Signals, route guards |
| **Senior Angular Developer** | TypeScript typing, component lifecycle, form handling | Reactive forms, subscriptions cleanup, HTTP interceptors |
| **UX Architect** | Ergonomics, user journey, navigation clarity, accessibility | Information architecture, ARIA attributes, UX flow |
| **Senior UI-UX Developer** | CSS/SCSS design system, dynamic layouts, micro-animations | Design tokens, responsive breakpoint flex/grid, loaders |
| **Performance Engineer** | API latency, DB query execution plan, JS bundle size | Indexing, payload size, memory leaks, lazy loading |
| **QA Lead** | Test coverage, edge-case resilience, defect severity | Unit tests (`dotnet test`, `npm test`), input validations |

---

## 🛠️ Step-by-Step Execution Instructions for Solution Analysis

### Phase 1: Automated Health & Verification Baseline
Before starting manual code inspections, execute baseline verification commands to ensure the solution builds cleanly and tests pass:

1. **Verify Backend Build & Dependencies**:
   ```bash
   dotnet build FrostTrack.sln
   ```
2. **Execute Backend Unit Tests**:
   ```bash
   dotnet test FrostTrack.sln
   ```
3. **Verify Frontend Dependencies & Build**:
   ```bash
   cd frosttrack.client
   npm run build
   ```
4. **Execute Frontend Unit Tests & Linting**:
   ```bash
   cd frosttrack.client
   npm test -- --watch=false
   npm run lint
   ```

---

### Phase 2: Layer-by-Layer Code & Architecture Audit

#### 1. Backend (.NET 8/9 Clean Architecture) Audit Instructions
- Inspect `Domain/` project:
  - Verify all domain entities exist under `Domain/Entities/` (e.g., `Booking.cs`, `Delivery.cs`, `RecurringCharge.cs`, `DailyStockBook.cs`, `StockReport.cs`).
  - Confirm legacy POS entities have been purged.
- Inspect `Application/` project:
  - Check command/query services and validation logic (`Application/Services/`, `Application/Contractors/`).
  - Ensure DTO mappings prevent over-posting or leaking internal entity structures.
- Inspect `Persistence/` project:
  - Check `Persistence/Contexts/` for DbContext configuration and migration history.
  - Audit EF Core query efficiency (look for missing `.AsNoTracking()`, un-indexed queries, sync-over-async calls).
- Inspect `FrostTrack.Server/` project:
  - Audit controllers under `Controllers/` for proper authorization attributes (`[Authorize]`), clean status codes, and global exception handling middleware.

#### 2. Frontend (Angular 17 Client) Audit Instructions
- Inspect `frosttrack.client/src/app/`:
  - Review feature organization under `administration`, `authentication`, `booking`, `common`, `dashboard`, `delivery`, `layout`, `recurring-charge`, `stock`, `shared`.
  - Confirm Standalone component conversion status.
  - Audit state management (usage of Signals vs RxJS Observables).
  - Verify memory leak safety: search for `.subscribe()` calls without `takeUntilDestroyed()` or unsubscriptions.
- Review UI/UX & Styling:
  - Check design tokens and styles in `styles.scss` / Bootstrap / Angular Material integrations.
  - Inspect visual feedback (toast notifications via `ngx-toastr`, alert dialogs via `SweetAlert2`, charts via `ApexCharts`/`ECharts`).

#### 3. Cold Storage Business Domain Validation Instructions
- Verify the 6 core cold storage operational workflows:
  1. **Customer & Booking Management**: Creating bookings with chamber temperature specs, commodity type, and capacity reservation.
  2. **Inward Receiving & Delivery**: Processing inward gate receipts, assigning chamber/rack IDs, gross/net weight logs.
  3. **Daily Stock Book**: Daily stock balance tracking ($\text{Opening} + \text{Inward} - \text{Outward} = \text{Closing}$).
  4. **Recurring Storage Charges**: Automatic recurring charge generation based on storage rate, duration, and volume/weight.
  5. **Outward Dispatch**: Processing stock release requests and generating dispatch gate passes.
  6. **Asset & Branch Management**: Multi-branch support, cold room asset tracking, company & bank configurations.

---

## 📊 Phase 3: Reporting & Action Plan Generation

When completing the solution analysis, compile a comprehensive analysis document structured into:
1. **Executive Summary & Health Matrix**: Scores (A to F) across Architecture, Domain, UI/UX, Performance, QA.
2. **Multi-Role Findings**: Section per role group (.NET, Angular, UX/UI, Cold Storage Domain, Performance/QA).
3. **Identified Anti-Patterns & Code Smells**: Explicit file links with line numbers (`file:///path/to/file.cs#L10`).
4. **Prioritized Action Plan**:
   - **P0 (Immediate Blockers)**: Security gaps, build failures, data corruption risks, severe memory leaks.
   - **P1 (High Priority)**: Performance bottlenecks, missing validation, UX usability issues, un-indexed DB queries.
   - **P2 (Medium Priority)**: Architectural refactoring, styling polish, test coverage enhancements.

---

## ⚡ Using Skill Shortcuts for Analysis

You can invoke specialized analysis skill files directly:
- `/skill solution-analysis`: Run complete end-to-end multi-role audit.
- `/skill dotnet-architecture-audit`: Deep dive into backend .NET & EF Core code.
- `/skill angular-ux-audit`: Deep dive into Angular 17 client & UI/UX ergonomics.
- `/skill cold-storage-domain-audit`: Audit business workflows & cold storage operations.
- `/skill qa-performance-audit`: Audit quality assurance, test coverage, and performance.
