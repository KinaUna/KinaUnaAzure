using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            logger.LogError(
                exception, "Exception occurred: {Message}", exception.Message);

            ProblemDetails problemDetails = new()
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error"
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }

    internal sealed class AuthenticationExceptionHandler(ILogger<AuthenticationExceptionHandler> logger) : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            logger.LogError(
                exception, "Exception occurred: {Message}", exception.Message);

            logger.LogError(
                exception, "Exception occurred: {Message}", exception.Message);

            // Redirect to the home page if an authentication exception occurs
            httpContext.Response.Redirect("/Account/LogOut");
            return ValueTask.FromResult(true);
        }
    }
}
