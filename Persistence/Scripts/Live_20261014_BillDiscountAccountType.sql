-- Trial Balance: show "বিল সমন্বয়" in the Account Type column for বিল ডিসকাউন্ট / অ্যাডজাস্টমেন্ট.
-- The bill discount head is a system head (not on the Transaction Head screen), so it is put in its own
-- Transaction Group here. Run after Live_20261014_AddAccountTypeToTransactionGroup.sql. Safe to run more than once.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @TenantId uniqueidentifier = (SELECT TOP 1 TenantId FROM Companies WHERE IsActive = 1 ORDER BY Id);
IF @TenantId IS NULL THROW 50000, 'No active company found', 1;

DECLARE @GroupId uniqueidentifier =
    (SELECT TOP 1 Id FROM finance.TransactionGroups WHERE TenantId = @TenantId AND Code = N'TG-BILL-DISCOUNT');

IF @GroupId IS NULL
BEGIN
    SET @GroupId = NEWID();
    INSERT INTO finance.TransactionGroups (Id, Code, Name, SortOrder, Description, IsActive, AccountType,
                                           TenantId, CreatedById, CreatedTime, IsDeleted, IsArchived)
    VALUES (@GroupId, N'TG-BILL-DISCOUNT', N'বিল ডিসকাউন্ট / অ্যাডজাস্টমেন্ট',
            (SELECT ISNULL(MAX(SortOrder), 0) + 1 FROM finance.TransactionGroups WHERE TenantId = @TenantId AND IsDeleted = 0),
            N'Bill discounts and adjustments (system head)', 1, N'বিল সমন্বয়',
            @TenantId, 1, SYSUTCDATETIME(), 0, 0);
END
ELSE
    UPDATE finance.TransactionGroups SET AccountType = N'বিল সমন্বয়', IsDeleted = 0, IsActive = 1
    WHERE Id = @GroupId;

UPDATE finance.TransactionHeads
SET TransactionGroupId = @GroupId
WHERE TenantId = @TenantId AND UsageFor = 'BILL_DISCOUNT' AND IsDeleted = 0;

COMMIT TRANSACTION;

SELECT g.Name AS GroupName, g.AccountType, h.Name AS HeadName
FROM finance.TransactionHeads h
JOIN finance.TransactionGroups g ON g.Id = h.TransactionGroupId
WHERE h.TenantId = @TenantId AND h.UsageFor = 'BILL_DISCOUNT';
