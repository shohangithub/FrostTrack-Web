public record TransactionPaginationQuery(
    int PageSize, int PageIndex, string? OrderBy, bool? IsAscending, string? OpenText, DateOnly? DateFrom, DateOnly? DateTo,
    string? UsageFor = null,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);