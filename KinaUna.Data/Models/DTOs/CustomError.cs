
#nullable enable
using Microsoft.Extensions.Logging;

namespace KinaUna.Data.Models.DTOs;

public class CustomError
{
    public static readonly string UndefinedErrorCode = "UndefinedError";
    public static readonly string ValidationErrorCode = "ValidationError";
    public static readonly string HttpRequestErrorCode = "HttpRequestError";
    public static readonly string NotFoundErrorCode = "NotFoundError";
    public static readonly string ExceptionErrorCode = "ExceptionError";
    public static readonly string UnauthorizedErrorCode = "UnauthorizedError"; 

    private CustomError(string code, string message, ILogger? logger = null)
    {
        Code = code;
        Message = message;
        Logger = logger;
    }

    public string Code { get; init; }
    public string Message { get; init; }
    public ILogger? Logger { get; init; }


    public static readonly CustomError None = new(string.Empty, string.Empty);
    public static readonly CustomError UndefinedError = new(UndefinedErrorCode, "An unknown error occurred.");
    public static CustomError ValidationError(string message, ILogger? logger = null) => new(ValidationErrorCode, message, logger);
    public static CustomError HttpRequestError(string message, ILogger? logger = null) => new(HttpRequestErrorCode, message, logger);
    public static CustomError NotFoundError(string message, ILogger? logger = null) => new(NotFoundErrorCode, message, logger);
    public static CustomError ExceptionError(string message, ILogger? logger = null) => new(ExceptionErrorCode, message, logger);
    public static CustomError UnauthorizedError(string message, ILogger? logger = null) => new(UnauthorizedErrorCode, message, logger);
}