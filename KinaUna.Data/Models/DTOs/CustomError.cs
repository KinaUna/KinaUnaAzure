
#nullable enable
using Microsoft.Extensions.Logging;

namespace KinaUna.Data.Models.DTOs;

public class CustomError
{
    private const string ValidationErrorCode = "ValidationError";
    private const string HttpRequestErrorCode = "HttpRequestError";
    private const string NotFoundErrorCode = "NotFoundError";
    private const string ExceptionErrorCode = "ExceptionError";

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
    public static CustomError ValidationError(string message, ILogger? logger = null) => new(ValidationErrorCode, message, logger);
    public static CustomError HttpRequestError(string message, ILogger? logger = null) => new(HttpRequestErrorCode, message, logger);
    public static CustomError NotFoundError(string message, ILogger? logger = null) => new(NotFoundErrorCode, message, logger);
    public static CustomError ExceptionError(string message, ILogger? logger = null) => new(ExceptionErrorCode, message, logger);
}