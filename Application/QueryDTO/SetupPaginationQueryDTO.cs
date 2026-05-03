/// <summary>
/// Shared pagination query for all setup/master-data entities that support archive and soft-delete lifecycle.
/// archiveStatus: "active" (default) | "archived" | "deleted"
/// </summary>
public record SetupPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);
