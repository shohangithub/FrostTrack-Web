using Microsoft.EntityFrameworkCore;

namespace Application.Framework;

public record struct RequestResponse<T>(T Payload, bool IsSuccess = true, HttpStatusCode HttpStatusCode = HttpStatusCode.OK);
public record struct ErrorResponse(Dictionary<string, string[]>? Errors, string Type, string Title, int Status, string Message, string TraceId);

public record struct PagingResponse(bool HasNextPage, bool HasPreviousPage, int PageSize, int PageIndex, int TotalData, int TotalPages);
public struct PaginationResult<T>
{
    #region properties
    public PagingResponse Paging { get; }
    public IEnumerable<T> Data { get; }

    #endregion

    private PaginationResult(List<T> data, int pageIndex, int pageSize, int totalData)
    {
        var notZeroPageSize = pageSize == 0 ? 1 : pageSize;
        var totalPages = (totalData / notZeroPageSize) + ((totalData % notZeroPageSize) == 0 ? 0 : 1);
        Data = data ?? new();
        Paging = new PagingResponse(HasNextPage: ((pageIndex + 1) * pageSize) < totalData,
                                    HasPreviousPage: pageIndex > 0,
                                    PageSize: pageSize,
                                    PageIndex: pageIndex,
                                    TotalData: totalData,
                                    TotalPages: totalPages);
    }

    public static async ValueTask<PaginationResult<T>> CreateAsync(IQueryable<T>? query, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (query is null || pageIndex < 0 || pageSize == 0) return default;

        var totalCount = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return new(items, pageIndex, pageSize, totalCount);
    }
}
