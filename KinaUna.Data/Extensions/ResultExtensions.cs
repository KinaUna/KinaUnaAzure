using System;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace KinaUna.Data.Extensions
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Bind function for CustomResult. To chain multiple operations.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="result"></param>
        /// <param name="bind"></param>
        /// <returns></returns>
        public static CustomResult<TOut> Bind<TIn, TOut>(this CustomResult<TIn> result, Func<TIn, CustomResult<TOut>> bind)
        {
            return result.IsSuccess ? bind(result.Value) : CustomResult<TOut>.Failure(result.Error?? CustomError.UndefinedError );
        }

        /// <summary>
        /// TryCatch function for CustomResult. To catch exceptions in a function.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="result"></param>
        /// <param name="func"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static CustomResult<TOut> TryCatch<TIn, TOut>(this CustomResult<TIn> result, Func<TIn, TOut> func, CustomError error)
        {
            try
            {
                return result.IsSuccess ? CustomResult<TOut>.Success(func(result.Value)) : CustomResult<TOut>.Failure(result.Error ?? CustomError.UndefinedError);
            }
            catch (Exception e)
            {
                
                return CustomResult<TOut>.Failure(CustomError.ExceptionError(e.Message, result.Error?.Logger));
            }
        }

        public static IActionResult ToActionResult<T>(this CustomResult<T> result)
        {
            if (result == null)
            {
                return new NotFoundObjectResult("Result is null.");
            }

            if(result.IsSuccess)
            {
                return new OkObjectResult(result.Value);
            }

            if (!result.IsFailure) return new BadRequestObjectResult("Unknown error.");

            if(result.Error?.Code == CustomError.NotFoundErrorCode)
            {
                return new NotFoundObjectResult(result.Error.Message);
            }

            if (result.Error?.Code == CustomError.UnauthorizedErrorCode)
            {
                return new UnauthorizedObjectResult(result.Error.Message);
            }

            return new BadRequestObjectResult(result.Error?.Message);

        }
    }
}
