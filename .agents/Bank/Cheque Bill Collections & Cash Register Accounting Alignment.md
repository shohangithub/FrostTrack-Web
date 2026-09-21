# Implementation Plan: Bank/Cheque Bill Collections & Cash Register Accounting Alignment

Support customer due payments and bill collections via **Bank Transfer** and **Cheque**, enhance the **Bill Collection** and **Bank Transaction** forms, and correct the accounting logic in the **Cash Book (নগদ বই)** so physical cash, bank balances, and customer dues reconcile accurately.

---

## User Review Required

> [!IMPORTANT]
> **Separation of Cash Book (নগদ বই) and Bank Book (ব্যাংক বই):**
> 1. In [`CashBookService.cs`](file:///d:/Personel/FrostTrack_Web/Application/Services/CashBookService.cs), `GetOpeningBalanceAsync` was previously called with `includeBank: true`. This blended the bank opening balance into the physical Cash Book opening balance. We will change this to `includeBank: false` so that the **নগদ বই** strictly tracks physical cash in the office drawer.
> 2. For Bank Transactions touching the Cash Book:
>    - **Bank Deposit (Cash $\rightarrow$ Bank)**: Recorded as **Credit** (Cash Outflow / টাকা খরচ/ব্যাংকে জমা), reducing cash balance.
>    - **Bank Withdrawal (Bank $\rightarrow$ Cash)**: Recorded as **Debit** (Cash Inflow / ব্যাংক থেকে নগদ উত্তোলন), increasing cash balance.
> 3. Bill collections paid via **Bank Transfer / Cheque** will **NOT** appear in the Cash Book (since no physical cash entered the cash drawer), but will automatically create/update the corresponding **Bank Transaction** so it appears in the **Bank Book** and reduces the customer's due.

---

## Proposed Changes

### 1. Domain Entities & Database Schema

#### [MODIFY] [`BankTransaction.cs`](file:///d:/Personel/FrostTrack_Web/Domain/Entitites/General/BankTransaction.cs)
- Add `SourceType` (`"CASH"`, `"BILL_COLLECTION"`, `"EXTERNAL"`) to track the origin of the bank transaction.
- Add `TransactionId` (nullable `Guid`) to optionally link back to the finance `Transaction` (e.g. from Bill Collection).

#### [MODIFY] [`Transaction.cs`](file:///d:/Personel/FrostTrack_Web/Domain/Entitites/Transaction.cs)
- Add `BankId` (nullable `int`) and navigation property `Bank? Bank` so finance transactions paid via Bank/Cheque know which company bank account was used.

#### [NEW] Migration: `AddBankToTransactionAndSourceTypeToBankTransaction`
- Generate and apply migration for `BankTransaction.SourceType`, `BankTransaction.TransactionId`, and `Transaction.BankId`.

---

### 2. Application Layer (Backend Logic & DTOs)

#### [MODIFY] [`BillCollectionRequest.cs`](file:///d:/Personel/FrostTrack_Web/Application/RequestDTO/BillCollectionRequest.cs) & [`DeliveryBillCollectionRequest.cs`](file:///d:/Personel/FrostTrack_Web/Application/RequestDTO/DeliveryBillCollectionRequest.cs)
- Add `int? BankId` to the request records.

#### [MODIFY] [`BillCollectionService.cs`](file:///d:/Personel/FrostTrack_Web/Application/Services/BillCollectionService.cs)
- In `CreateBillCollectionAsync` and `CreateDeliveryBillCollectionAsync`:
  - Set `BankId` on the `Transaction` entity.
  - If `request.PaymentMethod != PaymentMethods.CASH` and `request.BankId.HasValue`:
    - Automatically create a `BankTransaction` (`TransactionType = "Deposit"`, `SourceType = "BILL_COLLECTION"`, `BankId`, `Amount`, `Reference = request.PaymentReference`, `Description = $"Bill Collection - ... ({request.PaymentMethod})"`).
    - Update the target Bank account's running balance (`BalanceAfter`).

#### [MODIFY] [`CashBookService.cs`](file:///d:/Personel/FrostTrack_Web/Application/Services/CashBookService.cs)
- Fix opening balance call: pass `includeBank: false` to `_balanceCalculatorService.GetOpeningBalanceAsync`.
- Only include `BankTransaction` records where `SourceType == "CASH"` (or legacy records that are internal cash contra).
- Invert the Debit/Credit logic for Cash Book:
  - `Deposit` (Cash $\rightarrow$ Bank) $\rightarrow$ **Credit** (Money OUT of cash drawer).
  - `Withdraw` (Bank $\rightarrow$ Cash) $\rightarrow$ **Debit** (Money IN to cash drawer).

---

### 3. Frontend Layer (Angular Client)

#### [MODIFY] [`bill-collection.component.html`](file:///d:/Personel/FrostTrack_Web/frosttrack.client/src/app/bill-collection/components/bill-collection/bill-collection.component.html) & [`.ts`](file:///d:/Personel/FrostTrack_Web/frosttrack.client/src/app/bill-collection/components/bill-collection/bill-collection.component.ts)
- Uncomment and enhance the `paymentMethod` dropdown (`CASH`, `BANK_TRANSFER`, `CHEQUE`).
- When `paymentMethod != 'CASH'`, display:
  - **Deposit Bank** dropdown (populated from `BankService.getLookup()`).
  - **Cheque / Reference Number** input (required when Bank/Cheque selected).
- Validate that `bankId` is selected when payment method is `BANK_TRANSFER` or `CHEQUE`.

#### [MODIFY] [`delivery-bill-collection.component.html`](file:///d:/Personel/FrostTrack_Web/frosttrack.client/src/app/bill-collection/components/delivery-bill-collection/delivery-bill-collection.component.html) & [`.ts`](file:///d:/Personel/FrostTrack_Web/frosttrack.client/src/app/bill-collection/components/delivery-bill-collection/delivery-bill-collection.component.ts)
- Add the same `paymentMethod` selector and conditional `bankId` / `paymentReference` fields.

#### [MODIFY] [`bank-transaction.component.html`](file:///d:/Personel/FrostTrack_Web/frosttrack.client/src/app/common/components/bank-transaction/bank-transaction.component.html) & [`.ts`](file:///d:/Personel/FrostTrack_Web/frosttrack.client/src/app/common/components/bank-transaction/bank-transaction.component.ts)
- Add **Deposit Source / Purpose** dropdown on the Deposit tab:
  - `Office Cash (নগদ তহবিল থেকে)` $\rightarrow$ `SourceType: "CASH"`
  - `External / Other Deposit (সরাসরি ব্যাংক জমা)` $\rightarrow$ `SourceType: "EXTERNAL"`

---

## Verification Plan

### Automated Tests / Code Checks
- Run `dotnet build` to ensure 0 compilation errors across all projects.
- Verify EF Core migration applies cleanly.

### Manual Verification Flow
1. **Scenario 1: Customer Bill Collection via Cheque**:
   - Open `/bill-collection/new`.
   - Select a booking with due.
   - Choose `Payment Method = Bank Cheque`, select a Bank, enter Cheque `#CHQ-9901`.
   - Submit.
   - **Verify**: Customer due is reduced.
   - **Verify**: Bank Book (`/reports/bankbook`) shows the ৳ amount under the selected bank.
   - **Verify**: Cash Book (`/reports/cashbook`) does **NOT** show this transaction (Cash drawer balance unaffected).

2. **Scenario 2: Cash Deposit to Bank**:
   - Open `/common/bank-transaction`.
   - Create Deposit with `Source = Office Cash (নগদ তহবিল থেকে)`.
   - **Verify**: Cash Book (`/reports/cashbook`) shows it as **Credit** (Cash Out) and correctly deducts from the cash balance.
   - **Verify**: Bank Book (`/reports/bankbook`) shows it as **Debit** (Bank In) and adds to the bank balance.
