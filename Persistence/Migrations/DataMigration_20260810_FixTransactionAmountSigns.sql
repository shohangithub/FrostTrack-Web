-- Migration to fix negative transaction amounts
-- Due to a bug in TransactionService, DEBIT transactions were stored as negative values,
-- causing double-negative issues in reporting. This script converts all negative amounts to positive.

BEGIN TRANSACTION;

-- Fix negative amounts in Transactions
UPDATE finance.Transactions
SET Amount = ABS(Amount),
    NetAmount = ABS(NetAmount)
WHERE Amount < 0 OR NetAmount < 0;

-- Optional: Verify if there are any BankTransactions that were improperly stored
-- (Bank transactions were verified to be stored properly, but just in case)
UPDATE finance.BankTransactions
SET Amount = ABS(Amount)
WHERE Amount < 0;

COMMIT TRANSACTION;
