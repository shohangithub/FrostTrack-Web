---
name: qa-performance-audit
description: Quality assurance and performance engineering audit skill for backend API throughput, DB indexing, bundle optimization, and test coverage.
---

# QA & Performance Engineering Audit Skill

## Objective
Audit the system performance, scalability bottlenecks, database execution efficiency, test coverage, and software quality assurance matrix for FrostTrack Web.

## Audit Directives

### 1. Performance Engineering Protocols
- **Database Query & Profiling**:
  - Scan for un-indexed `JOIN` keys, `WHERE` clauses filtering by `DateTime` or string searches without index support.
  - Evaluate payload size: check for excessive DTO field serialization or heavy object graphs.
  - Identify blocking sync-over-async calls (`Task.Wait()`, `.Result`).
- **Frontend Performance**:
  - Check Angular bundle budgets in `angular.json` (Main bundle < 2MB initial load).
  - Verify image asset optimization (WebP/SVG formats, lazy loading images).
  - Check DOM tree depth and change detection execution frequency.

### 2. Quality Assurance & Testing Matrix
- **Test Suite Verification**:
  - Audit C# Unit/Integration tests (`dotnet test`).
  - Audit Angular frontend unit tests (`npm test`).
  - Identify critical un-tested application services and UI components.
- **Edge-Case & Resilience Testing**:
  - Input validation boundaries (e.g., negative stock weights, invalid date ranges, overlapping booking slots).
  - Concurrency handling (simultaneous stock updates, race conditions).
  - Global error trapping (API 500 error handling, graceful network disconnection UI notices).

---

## Evaluation Output Template
```markdown
## ⚡ QA & Performance Engineering Audit Findings

### Benchmark & Performance Metrics
| Metric Area | Target | Current Status | Assessment |
| :--- | :---: | :---: | :---: |
| **API Response Time (P95)** | < 200 ms | ... | 🟢 Pass / 🔴 Slow |
| **Initial JS Bundle Size** | < 2.0 MB | ... | 🟢 Pass / 🟡 Needs Optimization |
| **Backend Unit Test Coverage** | > 80% | ... | 🔴 Coverage Gap |
| **Frontend Unit Test Coverage** | > 70% | ... | 🔴 Coverage Gap |

### High-Priority Performance & QA Defect Matrix
1. **[Defect Title]**
   - **Type**: Performance / Bug / Test Gap
   - **Severity**: P0 / P1 / P2
   - **Impact**: ...
   - **Remediation Plan**: ...
```
