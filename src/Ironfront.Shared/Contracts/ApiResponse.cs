namespace Ironfront.Shared.Contracts;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string errorCode, string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}