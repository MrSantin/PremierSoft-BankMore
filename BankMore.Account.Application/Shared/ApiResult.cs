using System.Net;
using System.Text.Json.Serialization;

namespace BankMore.Account.Application.Shared;

public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public HttpStatusCode Status { get; init; }
    public string? Type { get; init; }
    public string? Message { get; init; }
    public object? Data { get; init; }
    
    [JsonConstructor]
    private ApiResult(
        bool success,
        HttpStatusCode status,
        object? data = null,
        string? message = null,
        string? type = null)
    {
        Success = success;
        Status = status;
        Data = data;
        Message = message;
        Type = type;
    }

    public static ApiResult<T> Ok(object? data = null)
        => new(true, HttpStatusCode.OK, data);

    public static ApiResult<T> NoContent()
        => new(true, HttpStatusCode.NoContent);

    public static ApiResult<T> Fail(HttpStatusCode status, string type, string message = "")
        => new(false, status, null, message, type);
}
