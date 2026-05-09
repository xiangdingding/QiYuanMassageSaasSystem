namespace MassageSaas.Shared.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public static PagedResult<T> Empty(int page, int pageSize)
        => new(Array.Empty<T>(), 0, page, pageSize);
}

public record PageQuery(int Page = 1, int PageSize = 20, string? Keyword = null)
{
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize switch
    {
        < 1 => 20,
        > 200 => 200,
        _ => PageSize
    };
}

public record ApiError(string Code, string Message);
