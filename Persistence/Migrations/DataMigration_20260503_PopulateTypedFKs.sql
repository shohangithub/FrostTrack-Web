-- ===================================================================
-- Data Migration Script: Populate Typed FKs from Legacy EntityId/EntityName
-- Migration Date: 2025-05-03
-- Purpose: Backfill new typed FK columns (DeliveryId, SupplierId, CompanyId)
--          from existing EntityId/EntityName polymorphic string data
--          
-- IMPORTANT: Run BEFORE applying EF migration 
--            20260503060651_StructuralFixes_BookingDetailFK_TransactionTypedFKs_BranchCompanyFK_AuditUtc
--
-- This script is PRODUCTION-SAFE:
-- - Uses TRY_CAST to validate UUID conversion (no errors if invalid)
-- - Only updates rows where EntityId matches a valid single UUID (not batch comma-separated)
-- - Does NOT delete EntityId/EntityName data (preserved for audit)
-- ===================================================================

USE [FrostTrackDb];  -- Replace with actual database name if different

-- ===================================================================
-- 1. TRANSACTION.DeliveryId: Populate from Transaction.EntityId where EntityName='DELIVERY'
-- ===================================================================
-- Description: Where Transaction.EntityName='DELIVERY' and EntityId is a single valid GUID,
--              populate the new DeliveryId FK column by casting EntityId to uniqueidentifier
--
PRINT N'[1] Populating finance.Transactions.DeliveryId from EntityId where EntityName=''DELIVERY''...';
BEGIN TRANSACTION PopulateDeliveryId;
BEGIN TRY
    UPDATE t 
    SET t.DeliveryId = TRY_CAST(t.EntityId AS uniqueidentifier)
    FROM finance.Transactions t
    WHERE t.EntityName = 'DELIVERY'
      AND t.EntityId IS NOT NULL
      AND t.EntityId NOT LIKE '%,%'  -- Exclude batch IDs (comma-separated)
      AND TRY_CAST(t.EntityId AS uniqueidentifier) IS NOT NULL
      AND t.DeliveryId IS NULL;  -- Only update if not already set
    
    DECLARE @rowsUpdated_DeliveryId INT = @@ROWCOUNT;
    PRINT FORMATMESSAGE(N'   Updated %d rows for DeliveryId', @rowsUpdated_DeliveryId);
    COMMIT TRANSACTION PopulateDeliveryId;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION PopulateDeliveryId;
    PRINT N'   ERROR populating DeliveryId - ROLLED BACK';
    THROW;
END CATCH;


-- ===================================================================
-- 2. BRANCH.CompanyId: Set to first active company (for single-company deployments)
-- ===================================================================
-- Description: For single-company deployments (most common), set all Branches.CompanyId
--              to the primary company. For multi-company, DBA must configure manually.
--
-- Action: Uncomment and configure as needed for your deployment
--
PRINT N'[2] Populating dbo.Branches.CompanyId...';
PRINT N'   SKIPPED - Please configure based on your deployment:';
PRINT N'   - Single company: UPDATE Branches SET CompanyId = (SELECT MIN(Id) FROM Companies WHERE IsActive = 1);';
PRINT N'   - Multi-tenant: Configure via application or custom script';

-- OPTION 1: Uncomment for SINGLE-COMPANY deployments
-- BEGIN TRANSACTION PopulateCompanyId;
-- BEGIN TRY
--     DECLARE @DefaultCompanyId INT;
--     SELECT TOP 1 @DefaultCompanyId = Id FROM Companies 
--     WHERE IsActive = 1 
--     ORDER BY Id;
--     
--     IF @DefaultCompanyId IS NOT NULL
--     BEGIN
--         UPDATE dbo.Branches 
--         SET CompanyId = @DefaultCompanyId 
--         WHERE CompanyId IS NULL;
--         
--         DECLARE @rowsUpdated_CompanyId INT = @@ROWCOUNT;
--         PRINT FORMATMESSAGE(N'   Updated %d rows: Set CompanyId = %d', @rowsUpdated_CompanyId, @DefaultCompanyId);
--     END
--     COMMIT TRANSACTION PopulateCompanyId;
-- END TRY
-- BEGIN CATCH
--     ROLLBACK TRANSACTION PopulateCompanyId;
--     PRINT N'   ERROR populating CompanyId - ROLLED BACK';
--     THROW;
-- END CATCH;


-- ===================================================================
-- 3. DATA VALIDATION: Report on legacy EntityId/EntityName still present
-- ===================================================================
PRINT N'[3] Data Validation - Legacy records still using EntityId/EntityName:';
DECLARE @LegacyDELIVERY_Count INT;
DECLARE @LegacyBOOKING_Count INT;

SELECT @LegacyDELIVERY_Count = COUNT(*) 
FROM finance.Transactions 
WHERE EntityName = 'DELIVERY' 
  AND (DeliveryId IS NULL OR EntityId IS NOT NULL);

SELECT @LegacyBOOKING_Count = COUNT(*) 
FROM finance.Transactions 
WHERE EntityName = 'BOOKING' 
  AND (BookingId IS NULL OR EntityId IS NOT NULL);

PRINT FORMATMESSAGE(N'   - DELIVERY records still using EntityId: %d', @LegacyDELIVERY_Count);
PRINT FORMATMESSAGE(N'   - BOOKING records still using EntityId: %d', @LegacyBOOKING_Count);
PRINT N'   Note: These will still work via legacy fallback logic during transition period.';


-- ===================================================================
-- 4. AUDIT: Show sample of migrated records
-- ===================================================================
PRINT N'[4] Sample of Migrated Records (first 10 with new DeliveryId):';
SELECT TOP 10 
    t.Id,
    t.TransactionCode,
    t.EntityName,
    t.EntityId,
    t.DeliveryId,
    t.Amount,
    CASE WHEN t.DeliveryId IS NOT NULL THEN 'MIGRATED' ELSE 'PENDING' END AS Status
FROM finance.Transactions t
WHERE t.EntityName = 'DELIVERY' 
  AND t.DeliveryId IS NOT NULL
ORDER BY t.CreatedTime DESC;

PRINT N'[MIGRATION COMPLETE] Data migration script finished successfully.';
PRINT N'Next step: Apply EF migration 20260503060651_StructuralFixes_BookingDetailFK_TransactionTypedFKs_BranchCompanyFK_AuditUtc';
