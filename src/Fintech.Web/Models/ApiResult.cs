namespace Fintech.Web.Models;

public sealed record ApiResult<T>(bool IsSuccess, T? Value, int? StatusCode, string? Error)
{
    public static ApiResult<T> Success(T value) => new(true, value, null, null);
    public static ApiResult<T> Failure(int statusCode, string error) => new(false, default, statusCode, error);
}
