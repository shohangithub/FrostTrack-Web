# Balance Sheet – Implementation Plan

Goal: a balance sheet for any date (and the existing two-date comparison) that matches the
auditor's *Statement of Financial Position* format, balances without a plug figure, and needs
only a one-time setup from the client.

---

## 1. Problems in the current report (`Application/Services/BalanceSheetService.cs`)

| # | Problem | Effect |
|---|---------|--------|
| 1 | Retained earnings = all money-in heads − all money-out heads | Loans received (e.g. "এম ডি স্যার গ্রহণ"), customer payments and capital are counted as **income** |
| 2 | Customer payments are income **and** customer dues are an asset | Rent is counted twice (once when it is due, once when it is paid) |
| 3 | No capital, creditors, deposits, provisions or retained earnings b/f | Can't match the auditor's figures |
| 4 | Fixed assets use `CurrentValue` typed by hand | No depreciation |
| 5 | "Owner's Capital" = Assets − (Liabilities + Equity) | Always balances, so errors are hidden |
| 6 | Customer dues are clamped at 0 and wrapped in `try { } catch { }` | Customer advances disappear; failures are silent |

---

## 2. Accounting rules the new report follows

* **Start date** – the date of the client's last audited balance sheet (e.g. 30.06.2025).
  Everything before it is summarised by the *opening entries*; everything after it is calculated.
* **Rent income is accrual based** – income = charges from the charge ledger (rent + labour +
  manual adjustments) − bill discounts, for the period. Customer payments are **not** income;
  they only turn a receivable into cash. This is what makes "customer dues" an honest asset.
* **Stock items** (cash, bank, customer balances, fixed assets) are calculated as of the report date.
* **Flow items** (head-based liabilities/assets/capital, profit) are summed from the start date
  to the report date, on top of their opening entry.
* **Equation**: Assets = Liabilities + Equity. If it doesn't hold, a red **"Difference"** row is shown
  (no plug).

### Balance sheet layout

```
ASSETS
  Non-current assets
    Property, Plant & Equipment (WDV)        ← asset register + depreciation
  Current assets
    Advance, Deposit & Pre-payments          ← ASSET heads + opening/adjustment entries
    Accounts Receivable (customer dues)      ← customers with a positive balance
    Cash in Hand                             ← existing cash calculation
    Cash at Bank (one row per bank)          ← existing bank calculation
EQUITY & LIABILITIES
  Equity
    Share Capital                            ← CAPITAL heads + entries
    Retained Earnings b/f                    ← opening entry
    Net Profit / (Loss) – current year       ← profit from FY start to report date
    (profit between start date and FY start is added to Retained Earnings b/f)
  Current liabilities
    Sundry Creditors / Loans                 ← LIABILITY heads (in − out) + entries
    Customer Advances                        ← customers with a negative balance
    Provisions & Accrued Expenses            ← entries
    Accounts Payable (credit purchases)      ← existing CREDIT-method sum
  [Difference]                               ← only if not balanced
```

---

## 3. Work items

### Step 1 – Account category on Transaction Heads
**Backend**
* `TransactionHead.AccountCategory` (`string?`, max 50) + constants class `AccountCategories`:
  `INCOME`, `EXPENSE_DIRECT`, `EXPENSE_ADMIN`, `EXPENSE_FINANCE`, `TAX`,
  `LIABILITY`, `ASSET`, `CAPITAL`, `FIXED_ASSET`.
* Resolution helper `AccountCategoryResolver.Resolve(head)`:
  * System usages are fixed and **excluded from profit**: `OPENING_BALANCE`, `CLOSING_BALANCE`,
    `CUSTOMER_PAYMENT`, `BILL_DISCOUNT`, legacy `BILL_COLLECTION`/`BOOKING`/`DELIVERY`/`DELEVERY`
    (settlement of receivables – to be verified against real data, see open questions).
  * If `AccountCategory` is null → money in = `INCOME`, money out = `EXPENSE_ADMIN`
    (so nothing changes for heads the client doesn't touch).
* Add the field to `TransactionHeadRequest` / `TransactionHeadResponse` / `TransactionHeadListResponse`,
  `TransactionHeadService` add/update, validator (must be one of the constants).
* Migration `AddAccountCategoryToTransactionHead`.

**Frontend**
* `add-transaction-head` form: "Account Category" dropdown (hidden for system heads).
* Transaction head list: category column.

### Step 2 – Company settings
* `Company.FiscalYearStartMonth` (`int`, default 7 = July).
* `Company.BalanceSheetStartDate` (`DateTime?`).
* Migration + fields on the company settings page.

### Step 3 – Opening Balances & Adjustments (new small CRUD, modelled on TransactionGroup)
**Entity** `finance.BalanceSheetEntries` (`AuditableEntity<Guid>`, tenant filter + soft delete)
| Field | Notes |
|-------|-------|
| `Code` | auto, prefix `BSE` |
| `EntryDate` | report includes entries with date ≤ report date |
| `EntryType` | `OPENING` (position at the start date) / `ADJUSTMENT` (year-end non-cash entry) |
| `Section` | `ASSET` / `LIABILITY` / `EQUITY` |
| `LineItem` | which row it adds to: `ADVANCE_DEPOSIT`, `SUNDRY_CREDITORS`, `PROVISION`, `SHARE_CAPITAL`, `RETAINED_EARNINGS`, `OTHER` |
| `AccountName` | free text, e.g. "WAPDA PDB deposit", "Provision – Electric bill" |
| `Amount` | signed, + increases the line |
| `ProfitCategory` | ADJUSTMENT only: the P&L side (e.g. `EXPENSE_DIRECT` for an electric bill provision) so profit and the liability move together |
| `Note` | |

* Repository registration, `IBalanceSheetEntryService`/service, `BalanceSheetEntryController`
  (`get-with-pagination`, get, post, put, delete), validator.
* Frontend page **Accounts → Opening Balances & Adjustments** (list + add/edit dialog), sidebar item,
  route. Help text: "Enter the figures from the last audited balance sheet once, dated the start date."

### Step 4 – Fixed asset depreciation
* `Asset`: add `OpeningWdv` (`decimal?`) and `OpeningWdvDate` (`DateTime?`) for assets bought before
  the software (the auditor's "WDV as on 01.07.2025"); new assets use `PurchaseCost` + `PurchaseDate`.
* `DepreciationCalculator.GetWdv(asset, asOfDate, fyStartMonth)` – reducing balance, matches the
  auditor's schedule: each completed fiscal year `WDV × rate`; the current partial year is pro-rated
  by month. Land (rate 0) never depreciates.
* Asset form: two new optional fields.
* Depreciation for the period is an expense line in profit.

### Step 5 – Rewrite `BalanceSheetService.GetBalanceSheetAsync`
1. Read company settings (start date, FY start month). If start date is not set → return the report
   with a warning flag ("Set up opening balances first").
2. **Cash / bank** – reuse `BalanceCalculatorService` (unchanged).
3. **Customers** – need the *signed* balance per customer, so add `NetBalance`
   (= accrued − paid − discount − advance, not clamped) to `CustomerDueSummaryResponse` in
   `BookingService.GetCustomerDueSummaryAsync`. Positive → receivable, negative → customer advances.
   Remove the silent `try/catch`.
4. **Fixed assets** – Σ `DepreciationCalculator.GetWdv`.
5. **Head-based lines** – group transactions (start date → report date) by resolved category:
   `LIABILITY` → creditors (in − out), `ASSET` → advances/deposits (out − in), `CAPITAL` → capital (in − out),
   `FIXED_ASSET` → ignored (the register is the source).
6. **Entries** – add `BalanceSheetEntries` (date ≤ report date) to their lines.
7. **Profit** (one private method, reused later by a P&L report):
   * Rent/charges income = ledger charges(report date) − ledger charges(start date) − discounts in period
     (needs a tenant-wide "total charges as of date" on the charge-ledger service).
   * \+ `INCOME` heads, − expense/tax heads, − depreciation, ± `ADJUSTMENT` entries' `ProfitCategory` side.
   * Split into *before current FY start* (goes to retained earnings b/f) and *current FY* (net profit line).
8. Totals, `Difference` = Assets − (Liabilities + Equity); delete the "Owner's Capital" plug.
9. Response: add `Difference`, `StartDate`, `IsConfigured`, and an optional `Section` per item
   (Non-current / Current) so the UI can print sub-totals. Comparative mode keeps working unchanged
   because it calls `GetBalanceSheetAsync` twice.

### Step 6 – Balance sheet page
* Auditor-style layout (sub-totals: Non-current, Current, Equity, Current liabilities).
* Red "Difference" row + hint when non-zero; banner + link to setup when not configured.
* Comparative mode: default dates = this FY end vs previous FY end.
* Print header with company name and "As at <date>".

---

## 4. Client setup (one time, ~15 minutes)
1. Company settings → start date `30.06.2025`, fiscal year starts July.
2. Transaction heads → set category on the few special heads
   (MD loan in/out → Liability, security deposits → Asset, asset purchases → Fixed Asset,
   electric/gas bill → Direct Expense, bank charges → Finance Expense).
3. Opening Balances page → from the 30.06.2025 audited statement: Share Capital 5,000,000;
   Retained Earnings b/f; Sundry Creditors; WAPDA 31,000 + Gas Fertilizer 10,000 deposits;
   provisions (Gas 1,850, Electric 97,856, Audit fee 25,300).
4. Assets → enter opening WDV per asset from the Annex-1 schedule.
5. Check that the balance sheet at 30.06.2025 shows **no Difference**. If it does, the cash, bank or
   customer opening balances in the software don't match the auditor's figures.

---

## 5. Open questions (decide before/during implementation)
1. **Legacy money-in usages** (`BOOKING`, `DELIVERY`, `BOOKING_EXTRA_CHARGE`): are they still customer
   payments that reduce dues, or real extra income? Check data before fixing their category.
2. **`EXTERNAL` bank transactions** without a linked finance transaction (bank-only income/expense):
   show them as "Unclassified bank movements" for now, or require a head?
3. **CREDIT payment method** (pay later): when the bill is paid later in cash, is it entered again
   under the same expense head? If yes the expense is counted twice; needs a settlement rule.
4. Should customer opening balances (`Customer.OpeningBalance`) dated before the start date be treated
   as part of the opening receivable (assumed yes)?

## 6. Out of scope (next phase, same foundation)
Income Statement (P&L) with date range, Statement of Changes in Equity, Cash Flow statement,
printable Notes and Annex-1 depreciation schedule.

## 7. Files touched (summary)
* Domain: `TransactionHead.cs`, `General/Company.cs`, `General/Asset.cs`, **new** `BalanceSheetEntry.cs`
* Persistence: `ApplicationDbContext.cs`, `DependencyInjection.cs`, 1 migration
* Application: TransactionHead DTOs/service/validator, Asset DTOs/service, `BookingService` (NetBalance),
  charge-ledger service (tenant-wide charges as of date), `BalanceSheetService`, `BalanceSheetResponse`,
  **new** BalanceSheetEntry DTOs/service/validator, `AccountCategoryResolver`, `DepreciationCalculator`,
  `DependencyInjection.cs`
* Server: **new** `BalanceSheetEntryController.cs`
* Client: transaction-head form/list, asset form, company settings, balance-sheet page,
  **new** balance-sheet-entry page + service + route, `sidebar-items.ts`
