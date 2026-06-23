namespace ATG.Platform.Application.Common;

public record Result<T>(bool IsSuccess, T? Data, string? Error)
{
    public static Result<T> Ok(T data) => new(true, data, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
