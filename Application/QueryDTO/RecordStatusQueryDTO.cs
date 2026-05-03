public record BookingPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);

public record DeliveryPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);

public record DeliveryChallanPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);
