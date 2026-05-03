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

public record SalesPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);

public record PurchasePaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);

public record ProductReceivePaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);

public record SaleReturnPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);

public record SupplierPaymentPaginationQuery(
    int PageSize,
    int PageIndex,
    string? OrderBy,
    bool? IsAscending,
    string? OpenText,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? Status = "active"
) : PaginationQuery(PageSize, PageIndex, OrderBy, IsAscending, OpenText, DateFrom, DateTo);
