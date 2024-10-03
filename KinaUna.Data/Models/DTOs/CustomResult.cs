#nullable enable
using System;
using Microsoft.Extensions.Logging;

namespace KinaUna.Data.Models.DTOs;

public class CustomResult<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public static implicit operator CustomResult<T>(T value) => Success(value);
    public static implicit operator CustomResult<T>(CustomError error) => Failure(error);

    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Value is not defined for failure.");
            }

            return _value!;
        }

        private init => _value = value;
    }

    public CustomError? Error { get; }

    private CustomResult(T value)
    {
        Value = value;
        IsSuccess = true;
        Error = CustomError.None;
    }

    private CustomResult(CustomError error)
    {
        if (error == CustomError.None)
        {
            throw new InvalidOperationException("Error must be defined for failure.");
        }

        IsSuccess = false;
        Error = error;
    }

    public static CustomResult<T> Success(T value) => new(value);

    public static CustomResult<T> Failure(CustomError error)
    {
        error.Logger?.LogError("{ErrorCode}: {ErrorMessage}", error.Code, error.Message);
        return new CustomResult<T>(error);
    }

    public static CustomResult<T> ExceptionCaughtFailure(Exception e, ILogger? logger = null)
    {
        return CustomResult<T>.Failure(CustomError.ExceptionError(e.Message, logger));
    }
}