namespace Application.ReponseDTO;

public record BalanceSheetEntryResponse(
    Guid Id,
    string Code,
    DateTime EntryDate,
    string EntryType,
    string LineItem,
    string AccountName,
    decimal Amount,
    string? ProfitCategory,
    string? Note
);

public record BalanceSheetSettingsResponse(
    DateTime? BalanceSheetStartDate,
    int FiscalYearStartMonth
);
