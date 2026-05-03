public record BankTransactionPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? transactionType = null,
    string? status = null,
    string? archiveStatus = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);
