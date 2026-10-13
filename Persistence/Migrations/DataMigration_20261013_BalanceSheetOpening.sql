-- Balance Sheet opening position for Sylhet Cold Storage and Ice Company Ltd.
-- Source: audited "Accounts- 30.06.2026.xls" (Statement of Financial Position, Notes 4–8, Annex-1),
-- column 30.06.2026. FrostTrack's cash book starts 01-07-2026 with cash in hand 20,621 = Note 6.
--
-- Safe to run more than once: every insert is skipped when the row already exists.
-- Run after migration 20261013063724_AddBalanceSheetSetup.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @CompanyId int, @TenantId uniqueidentifier;
SELECT TOP 1 @CompanyId = Id, @TenantId = TenantId FROM Companies WHERE IsActive = 1 ORDER BY Id;
IF @CompanyId IS NULL THROW 50000, 'No active company found', 1;

DECLARE @UserId int = 1, @BranchId int = (SELECT MIN(Id) FROM Branches), @Now datetime2 = SYSUTCDATETIME();
DECLARE @StartDate date = '2026-06-30';

-- ── 1. Start date and fiscal year (July–June) ─────────────────────────────
UPDATE Companies SET BalanceSheetStartDate = @StartDate, FiscalYearStartMonth = 7 WHERE Id = @CompanyId;

-- ── 2. Balance Sheet category of transaction heads ────────────────────────
UPDATE h SET AccountCategory = m.Category
FROM finance.TransactionHeads h
JOIN (VALUES
    -- Loans / sundry creditors (MD and other loans)
    (N'TH-260926-010', 'LIABILITY'),        -- এমডি স্যার গ্রহণ (paid to MD)
    (N'TH-260708-001', 'LIABILITY'),        -- এমডি স্যার থেকে ক্যাশ গ্রহণ (received from MD)
    (N'TH-260928-001', 'LIABILITY'),        -- এম. ডি স্যার প্রদান
    (N'TH-260729-001', 'LIABILITY'),        -- ঋণ গ্রহণ
    (N'TH-260806-001', 'LIABILITY'),        -- ঋণ পরিশোধ
    (N'TH-260927-001', 'LIABILITY'),        -- অতিরিক্ত বিল গ্রহণ (ভুলবশত) – owed back
    -- Direct expenses (Note 13: electricity, gas; labour)
    (N'TH-260729-002', 'EXPENSE_DIRECT'),   -- বিদ্যুৎ বিল
    (N'TH-260926-009', 'EXPENSE_DIRECT'),   -- লেবার বিল প্রদান
    -- Administrative expenses (Note 14)
    (N'TH-260926-003', 'EXPENSE_ADMIN'),    -- বেতন প্রদান
    (N'TH-260926-001', 'EXPENSE_ADMIN'),    -- বাজার খরচ
    (N'TH-260805-001', 'EXPENSE_ADMIN'),    -- স্টাফ খাবার বিল আদায় (recovery, reduces food cost)
    (N'TH-260725-001', 'EXPENSE_ADMIN'),    -- ইন্টারনেট বিল
    (N'TH-261005-002', 'EXPENSE_ADMIN'),    -- অফিস খরচ
    (N'TH-261004-001', 'EXPENSE_ADMIN'),    -- অফিস চা-নাস্তা
    (N'TH-261003-001', 'EXPENSE_ADMIN'),    -- অফিস প্রয়োজনীয় সরঞ্জাম
    (N'TH-260707-002', 'EXPENSE_ADMIN'),    -- কুরিয়ার খরচ
    (N'TH-260704-001', 'EXPENSE_ADMIN'),    -- বিবিধ ব্যয়
    (N'TH-261004-002', 'EXPENSE_ADMIN'),    -- ইলেকট্রিক মেরামত
    (N'TH-260709-001', 'EXPENSE_ADMIN'),    -- প্রয়োজনীয় কাঁচামাল ক্রয়
    (N'TH-261011-001', 'EXPENSE_ADMIN'),    -- ফ্লোর ও দেয়াল মেরামত
    (N'TH-261005-001', 'EXPENSE_ADMIN'),    -- যন্ত্রাংশ ক্রয়
    (N'TH-261004-003', 'EXPENSE_ADMIN'),    -- যন্ত্রাংশ মেরামত
    (N'TH-260926-002', 'EXPENSE_ADMIN'),    -- বিসিক শিল্প মালিক সমিতি
    (N'TH-260926-004', 'EXPENSE_ADMIN'),    -- মাসিক চাঁদা (বিসিক মসজিদ)
    -- Other income
    (N'TH-260926-007', 'INCOME'),           -- লেবার বিল গ্রহণ (আনলোড)
    (N'TH-260926-008', 'INCOME'),           -- লেবার বিল গ্রহণ (ডেলিভেরী)
    (N'TH-260926-006', 'INCOME'),           -- লেবার কমিশন (আনলোড)
    (N'TH-260926-005', 'INCOME'),           -- লেবার কমিশন (ডেলিভেরী)
    (N'TH-261005-003', 'INCOME'),           -- পুরাতন জিনিস বিক্রি
    (N'TH-260930-001', 'INCOME'),           -- বিবিধ আয়
    (N'TH-260924-001', 'INCOME')            -- পূর্বের বকেয়া এডজাস্ট
    -- Not set on purpose: TH-260707-001 ব্যাংক থেকে ক্যাশ উত্তোলন, TH-261011-002 ব্যাংকে জমা
    -- (cash ↔ bank transfers; record them in the Bank Book instead).
) AS m(Code, Category) ON m.Code = h.Code
WHERE h.TenantId = @TenantId;

-- ── 3. Bank account (Note 6.01) ───────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM Banks WHERE TenantId = @TenantId AND AccountNumber = N'4114-759977-000' AND IsDeleted = 0)
    INSERT INTO Banks (BankName, BankCode, AccountNumber, AccountTitle, OpeningBalance, CurrentBalance, Description,
                       IsMainAccount, IsActive, BranchId, TenantId, CreatedById, CreatedTime, IsArchived, IsDeleted)
    VALUES (N'AB Bank Ltd.', N'BNK-000001', N'4114-759977-000', N'Sylhet Cold Storage and Ice Company Ltd.', 6045, 6045,
            N'CD account. Opening balance from the audited statement as on 30-06-2026 (Note 6.01).',
            1, 1, @BranchId, @TenantId, @UserId, @Now, 0, 0);

-- ── 4. Fixed assets (Annex-1): Opening WDV = written down value as on 30-06-2026 ─
INSERT INTO Assets (AssetName, AssetCode, AssetType, PurchaseCost, CurrentValue, DepreciationRate, OpeningWdv, Condition,
                    Notes, IsActive, BranchId, TenantId, CreatedById, CreatedTime, IsArchived, IsDeleted)
SELECT a.Name, a.Code, a.Name, a.Wdv2025, a.Wdv2026, a.Rate, a.Wdv2026, N'Good',
       N'From audited fixed asset schedule (Annex-1): WDV 01-07-2025 ' + FORMAT(a.Wdv2025, 'N2') + N', WDV 30-06-2026 ' + FORMAT(a.Wdv2026, 'N2') + N'.',
       1, @BranchId, @TenantId, @UserId, @Now, 0, 0
FROM (VALUES
    (N'Land',                    N'AST-000001',  60721.00,  60721.00,  0),
    (N'Building',                N'AST-000002',  55040.00,  49536.00, 10),
    (N'Computer',                N'AST-000003',   5774.00,   4907.90, 15),
    (N'CC Camera',               N'AST-000004',   6798.00,   5778.30, 15),
    (N'Machinery',               N'AST-000005', 242136.00, 217922.40, 10),
    (N'Electrical Installation', N'AST-000006',   2515.00,   2137.75, 15),
    (N'Weighting Scale',         N'AST-000007',    128.00,    108.80, 15),
    (N'Furniture & Fixture',     N'AST-000008',    478.00,    430.20, 10),
    (N'Fire Extinguisher',       N'AST-000009',   4099.00,   3484.15, 15),
    (N'Office Instrument',       N'AST-000010',   6570.00,   5584.50, 15)
) AS a(Name, Code, Wdv2025, Wdv2026, Rate)
WHERE NOT EXISTS (SELECT 1 FROM Assets x WHERE x.TenantId = @TenantId AND x.AssetCode = a.Code);

-- ── 5. Opening balances (Statement of Financial Position, 30-06-2026) ─────
-- Retained earnings b/f is the balancing figure of the audited statement:
--   Assets 418,276 − Liabilities 21,504,717 − Share capital 5,000,000 = −26,086,441
-- (the statement prints −27,801,886 and Changes in Equity −28,499,308; neither balances it).
-- The audited statement has no customer dues; FrostTrack holds 362,623 of dues on 30-06-2026, added as a
-- separate retained earnings line (BSE-261013-009) so the balance sheet balances.
INSERT INTO finance.BalanceSheetEntries (Id, Code, EntryDate, EntryType, LineItem, AccountName, Amount, ProfitCategory, Note,
                                         TenantId, CreatedById, CreatedTime, IsDeleted, IsArchived)
SELECT NEWID(), e.Code, @StartDate, 'OPENING', e.LineItem, e.AccountName, e.Amount, NULL, e.Note,
       @TenantId, @UserId, @Now, 0, 0
FROM (VALUES
    (N'BSE-261013-001', 'SHARE_CAPITAL',     N'Issued, subscribed & paid up capital (10,000 shares @ Tk.500)', 5000000.00, N'Statement of Financial Position'),
    (N'BSE-261013-002', 'RETAINED_EARNINGS', N'Retained earnings as on 30-06-2026',  -26086441.00, N'Balancing figure of the audited statement (printed: -27,801,886)'),
    (N'BSE-261013-003', 'SUNDRY_CREDITORS',  N'Sundry creditors',                      21379711.00, N'Statement of Financial Position'),
    (N'BSE-261013-004', 'PROVISION',         N'Gas bill payable',                          1850.00, N'Note 8 – Liabilities for expenses'),
    (N'BSE-261013-005', 'PROVISION',         N'Electric bill payable',                    97856.00, N'Note 8 – Liabilities for expenses'),
    (N'BSE-261013-006', 'PROVISION',         N'Audit fees including VAT',                 25300.00, N'Note 8 – Liabilities for expenses'),
    (N'BSE-261013-007', 'ADVANCE_DEPOSIT',   N'WAPDA PDB, Sylhet',                        31000.00, N'Note 5 – Advances, deposits & pre-payments'),
    (N'BSE-261013-008', 'ADVANCE_DEPOSIT',   N'Natural Gas Fertilizer Factory Ltd.',      10000.00, N'Note 5 – Advances, deposits & pre-payments'),
    (N'BSE-261013-009', 'RETAINED_EARNINGS', N'Customer dues b/f (in FrostTrack, not in audited statement)', 362623.00,
     N'Customer dues on 30-06-2026 per FrostTrack (opening dues entered before go-live). Confirm with the accountant; re-check the amount before running on another database.')
) AS e(Code, LineItem, AccountName, Amount, Note)
WHERE NOT EXISTS (SELECT 1 FROM finance.BalanceSheetEntries x WHERE x.TenantId = @TenantId AND x.Code = e.Code);

COMMIT TRANSACTION;
