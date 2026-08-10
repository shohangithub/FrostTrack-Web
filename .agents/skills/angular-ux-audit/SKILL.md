---
name: angular-ux-audit
description: Frontend architectural & UI/UX evaluation skill for Angular 17 client, evaluating components, signals, RxJS streams, routing, state management, and modern aesthetics.
---

# Angular Architecture & UI/UX Audit Skill

## Objective
Audit the `frosttrack.client` Angular 17 project to ensure state-of-the-art frontend architecture, reactive performance, modularity, visual design excellence, and user interface ergonomics.

## Audit Directives & Inspection Areas

### 1. Angular Framework & Code Standards
- **Component Architecture**:
  - Verify adoption of Standalone components (`standalone: true`).
  - Check for `ChangeDetectionStrategy.OnPush` utilization to minimize change detection cycles.
  - Audit input/output signal APIs (`input()`, `output()`, `model()`).
- **RxJS & State Management**:
  - Verify memory leak safety: ensure all manually subscribed RxJS Observables utilize `takeUntilDestroyed()` or `async` pipes in templates.
  - Inspect HTTP Interceptors (`AuthInterceptor`, `ErrorInterceptor`) for request header injection and global error handling.
- **Routing & Modular Structure**:
  - Verify lazy loading of feature routes (`loadChildren`, `loadComponent`).
  - Ensure route guards (`auth.guard.ts`, `role.guard.ts`) secure authenticated views.

### 2. UI/UX & Visual Design Systems
- **Color Palette & Typography**:
  - Primary Theme: Deep Sapphire (`#0F172A`), Frost Cyan (`#06B6D4`), Cold Emerald (`#10B981`).
  - Typography: Modern sans-serif (Inter / Roboto / Outfit) with clear hierarchical sizing.
- **Visual Design & Aesthetics**:
  - Verify presence of modern UI elements: glassmorphism cards, subtle drop shadows, smooth hover transitions, clear active states.
  - Skeleton loaders for asynchronous data loading states (avoid blank screens or layout shifts).
- **Responsive Layout & Ergonomics**:
  - Mobile, tablet, and desktop layout adaptability via Bootstrap 5 grid & flexbox utility classes.
  - Accessible form validation with clear inline error messages and dynamic submit button state handling.

---

## Evaluation Output Template
```markdown
## 🎨 Angular Architecture & UI/UX Audit Findings

### Framework Compliance Scorecard
- **Standalone Components**: [Pass / Partial / Needs Work]
- **Signal / RxJS Hygiene**: [Pass / Fail - Memory leaks detected]
- **Lazy Loading**: [Pass / Fail]
- **Design & Polish**: [Pass / Needs Visual Enhancement]

### Specific Recommendations
1. **[Component/View Name]** (`file:///path/to/component.ts#L15`)
   - **Aspect**: UI / Architecture / RxJS / Accessibility
   - **Observation**: ...
   - **Suggested Improvement**:
     ```typescript
     // Refactored TypeScript / Component snippet
     ```
```
