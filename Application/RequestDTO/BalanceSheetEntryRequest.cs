namespace Application.RequestDTO;

public record BalanceSheetEntryRequest(
    DateTime EntryDate,
    string EntryType,
    string LineItem,
    string AccountName,
    decimal Amount,
    string? ProfitCategory,
    string? Note
);

public record BalanceSheetSettingsRequest(
    DateTime? BalanceSheetStartDate,
    int FiscalYearStartMonth
);
