---
name: solution-analysis
description: Multi-role skill to analyze the complete FrostTrack solution across .NET backend architecture, Angular frontend UI/UX, Cold Storage domain logic, performance, and QA.
---

# Solution Analysis Skill — FrostTrack (Cold Storage Limited)

## Objective
Provide an end-to-end, multi-perspective evaluation of the **FrostTrack Web** solution, acting on behalf of the 14 persona roles (Product Architect, System Designer, Product Designer, Project Manager, BA, Cold Storage Manager, .NET Architect, .NET Developer, Angular Architect, Angular Developer, UX Architect, UI-UX Developer, Performance Engineer, QA Lead).

## Workflow Execution Steps

### Step 1: Solution Topology Discovery
1. Inspect solution files (`FrostTrack.sln`, `FrostTrack.Server`, `Domain`, `Application`, `Infrastructure`, `Persistence`, `frosttrack.client`).
2. Identify all domain models, services, controllers, Angular components, routes, and shared utilities.
3. Map legacy POS code remnants vs active Cold Storage entities (`Booking`, `Delivery`, `RecurringCharge`, `Transaction`, `DailyStockBook`, `StockReport`, `Product`, `Customer`, etc.).

### Step 2: Multi-Role Evaluation Breakdown
Run deep inspections across 5 audit vectors:
- **Backend Audit**: Evaluate Clean Architecture compliance, dependency directions, EF Core DbContext configurations, async correctness, and API endpoints.
- **Frontend Audit**: Evaluate Angular 17 client structure, Standalone components, RxJS/Signals, route guards, state management, form validation, and CSS design system.
- **Domain & Operations Audit**: Verify Cold Storage operational logic (chamber allocation, temperature zoning, recurring charge calculation, daily stock audit, dispatch notes).
- **UI/UX & Aesthetics Audit**: Evaluate visual hierarchy, color palette, dark/light theme support, responsiveness, accessibility, and micro-interactions.
- **Performance & QA Audit**: Assess DB query efficiency, bundle footprint, test coverage, exception logging, and security posture.

### Step 3: Synthesis & Audit Output Generation
Generate a structured report structured into:
1. **Executive Summary & Health Scorecard** (Grade A-F per domain pillar).
2. **Key Strengths & Architectural Accomplishments**.
3. **Critical Architectural & Code Anti-Patterns** (with file location links and line numbers).
4. **Cold Storage Domain Operational Gap Analysis**.
5. **Prioritized Action Plan & Remediation Roadmap** (Immediate P0 fixes, Sprint P1 enhancements, Strategic P2 goals).

---

## Output Template Format
```markdown
# 📊 FrostTrack Solution Analysis Report

## Executive Summary & Health Scorecard
| Pillar | Rating | Status | Summary |
| :--- | :---: | :---: | :--- |
| **Backend Architecture (.NET Core)** | A- / B+ | 🟢 Healthy | ... |
| **Frontend Architecture (Angular 17)** | B | 🟡 Review | ... |
| **Cold Storage Domain Fit** | A | 🟢 Aligned | ... |
| **UI/UX Aesthetics & Accessibility** | B- | 🟡 Needs Polish | ... |
| **Performance & Scalability** | B | 🟡 Optimization | ... |
| **QA & Test Coverage** | C+ | 🔴 Gaps Identified | ... |

## Detailed Role Findings
### 🛡️ Backend Architecture (.NET Architect & Developer)
- [Findings with file links]

### 🎨 Frontend & UI/UX (Angular Architect, UX Architect, UI-UX Dev)
- [Findings with file links]

### ❄️ Cold Storage Business & Operations (Cold Storage Manager & BA)
- [Domain rule checks and findings]

### ⚡ Performance & Quality (Performance Engineer & QA Lead)
- [Query efficiency, bundle size, test coverage findings]

## 🚀 Prioritized Action Plan
1. **P0 (Critical)**: ...
2. **P1 (High)**: ...
3. **P2 (Medium)**: ...
```
