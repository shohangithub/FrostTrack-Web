---
name: dotnet-architecture-audit
description: Deep-dive code audit skill for .NET Clean Architecture, EF Core queries, domain models, security, and backend performance in FrostTrack.
---

# .NET Architecture & Code Audit Skill

## Objective
Audit the .NET Core backend (`FrostTrack.Server`, `Domain`, `Application`, `Infrastructure`, `Persistence`) for architectural integrity, clean layer isolation, EF Core performance, security, and enterprise C# standards.

## Audit Checklist & Inspection Directives

### 1. Clean Architecture Layer Isolation
- **Domain Layer (`Domain/`)**:
  - Verify entities inherit from base domain abstractions (`BaseEntity`, `AuditableEntity`).
  - Verify zero external framework references (No EF Core attributes or HTTP references in Domain).
  - Verify explicit domain enums (e.g., `BookingStatus`, `PaymentStatus`, `StockType`).
- **Application Layer (`Application/`)**:
  - Inspect application interfaces (`IApplicationDbContext`, `IStorageService`, etc.).
  - Check DTO design: ensure domain entities are never returned directly from API endpoints; proper AutoMapper / Mapster / projection mappings must exist.
  - Check command/query validation using FluentValidation.
- **Persistence Layer (`Persistence/`)**:
  - Inspect `DbContext` configurations in `Persistence/Contexts/`.
  - Ensure entity relationships (One-to-Many, Many-to-Many) are configured via `IEntityTypeConfiguration<T>` rather than inline attributes.
  - Verify soft-delete query filters (`HasQueryFilter(e => !e.IsDeleted)`).
- **Presentation Layer (`FrostTrack.Server/`)**:
  - Inspect API Controllers for lean logic (delegating work to Application layer services/mediators).
  - Verify HTTP status codes (`200 OK`, `201 Created`, `400 BadRequest`, `404 NotFound`, `500 ServerError`).

### 2. EF Core & Database Performance
- Check for missing `.AsNoTracking()` on read queries.
- Scan for potential N+1 queries (e.g., looping through child items without `.Include()` or projection).
- Inspect async usage (`await dbContext.Bookings.ToListAsync()`, avoiding `.Result` or `.Wait()`).
- Verify database indexing on foreign key properties and frequently filtered fields (`BranchId`, `CustomerId`, `BookingDate`).

### 3. Security & Resilience
- Verify JWT Authentication & Token Refresh mechanisms.
- Audit CORS configuration in `Program.cs` to prevent wildcards (`*`) in production settings.
- Ensure SQL Injection prevention (use of parameterized EF Core queries).
- Verify input sanitization and XSS protection on text fields.

---

## Evaluation Output Template
```markdown
## 🛡️ .NET Architecture Audit Findings

### Layering Compliance
- **Domain**: [Pass / Fail - Details]
- **Application**: [Pass / Fail - Details]
- **Persistence**: [Pass / Fail - Details]
- **Server API**: [Pass / Fail - Details]

### Code Quality & EF Core Bottlenecks
1. **[Issue Title]** (`file:///path/to/file.cs#L10-L25`)
   - **Severity**: High / Medium / Low
   - **Root Cause**: ...
   - **Recommended Code Fix**:
     ```csharp
     // Refactored code snippet
     ```
```
