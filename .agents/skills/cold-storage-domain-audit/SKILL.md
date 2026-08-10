---
name: cold-storage-domain-audit
description: Specialized cold storage domain verification skill evaluating booking, warehouse chamber/rack allocation, recurring charges, daily stock reporting, and stock lifecycle integrity.
---

# Cold Storage Domain Audit Skill

## Objective
Validate the business domain integrity and operational fidelity of **Cold Storage Limited**'s features in FrostTrack, ensuring complete coverage of warehouse logistics, inventory control, storage billing, and temperature-controlled storage tracking.

## Cold Storage Domain Audit Directives

### 1. Booking & Gate Receipt Workflow
- **Booking Management**:
  - Verify booking parameters: Customer reference, Commodity Type (Fruit, Meat, Dairy, Pharma), Storage Unit (Metric Tons, Bags, Pallets), Target Temperature Range (°C / °F), Start Date, Duration, Base Storage Rate.
  - Verify status transition model: `Draft` ➔ `Confirmed` ➔ `Active` ➔ `Completed` / `Cancelled`.
- **Inward Stock Receipt & Delivery**:
  - Validate inward receiving protocol: Vehicle No, Lot Numbering, Gross/Net Weight, Temperature at Gate, Quality Inspection status.
  - Verify chamber/rack allocation logic (assigning stock to specific Chamber IDs and Floor/Rack slots).

### 2. Daily Stock Book & Telemetry Controls
- **Daily Stock Reconciliation**:
  - Validate calculation model: $\text{Closing Stock} = \text{Opening Stock} + \text{Inward Quantity} - \text{Outward Quantity}$.
  - Ensure daily snapshot immutability (locking stock books after daily reconciliation).
- **Chamber & Rack Management**:
  - Track chamber capacity utilization (%) and rack-level occupancy.
  - Verify stock hold/quarantine flags (for spoiled, inspected, or disputed batches).

### 3. Storage Billing & Financial Engine
- **Recurring Storage Charge Calculation**:
  - Verify billing frequency models: Daily rate per MT/Bag vs Monthly slab rates.
  - Verify ancillary charge engine: Inward handling charge, Outward handling charge, Blast freezing fee, Re-packing fee.
- **Invoice & Transaction Accuracy**:
  - Verify recurring billing generator logic to prevent double billing or missed billing cycles.
  - Ensure payment status tracking (`Unpaid`, `Partially Paid`, `Paid`) linked to financial ledger transactions.

---

## Evaluation Output Template
```markdown
## ❄️ Cold Storage Domain Audit Findings

### Workflow Verification Matrix
| Business Workflow | Status | Functional Compliance | Risk Area |
| :--- | :---: | :--- | :--- |
| **Booking & Capacity** | 🟢 Complete | ... | ... |
| **Inward Stock Delivery** | 🟢 Complete | ... | ... |
| **Chamber / Rack Telemetry**| 🟡 Partial | ... | ... |
| **Daily Stock Book Audit** | 🟢 Complete | ... | ... |
| **Recurring Storage Billing**| 🟢 Complete | ... | ... |
| **Outward Dispatch Pass** | 🟢 Complete | ... | ... |

### Domain Logic Gaps & Recommendations
1. **[Domain Area]**
   - **Gaps Identified**: ...
   - **Business Impact**: ...
   - **Recommended Feature / Logic Fix**: ...
```
