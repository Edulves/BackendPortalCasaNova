namespace PainelFelizCasaNova.Api.Services;

/// <summary>
/// Exceção de API — o middleware traduz em envelope {code, message, detail}
/// igual ao handler DRF <c>core.exceptions.api_exception_handler</c>.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    /// <summary>Código de erro explícito (padrão: inferido do status).</summary>
    public string? Code { get; }

    /// <summary>Payload original do erro (flatten vira message). Ex.: {"new_password": [...]}.</summary>
    public object? Detail { get; }

    public ApiException(int statusCode, string message, string? code = null,
        object? detail = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        Code = code;
        Detail = detail;
    }
}

public static class ApiCodes
{
    public const string Ok = "ok";
    public const string Error = "error";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Throttled = "throttled";
    public const string ServerError = "server_error";
    public const string ValidationError = "validation_error";
    public const string InvalidToken = "invalid_token";

    public static string ParaStatus(int status) => status switch
    {
        401 => Unauthorized,
        403 => Forbidden,
        404 => NotFound,
        429 => Throttled,
        >= 500 => ServerError,
        >= 400 => ValidationError,
        _ => Error,
    };
}