BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014073332_AddAccountTypeToTransactionGroup'
)
BEGIN
    ALTER TABLE [finance].[TransactionGroups] ADD [AccountType] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261014073332_AddAccountTypeToTransactionGroup'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014073332_AddAccountTypeToTransactionGroup', N'9.0.9');
END;

COMMIT;
GO

