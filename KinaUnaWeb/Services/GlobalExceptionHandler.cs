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
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Log the exception with a warning level if it's an authentication exception, otherwise log it as an error
            if (exception.Message.EndsWith("invalid_grant"))
            {
                logger.LogInformation("Authentication invalid grant exception occurred: {Message}", exception.Message);
            }
            else
            {
                logger.LogError(
                    exception, "Exception occurred: {Message}", exception.Message);
            }
            
            // Redirect to the login page if an authentication exception occurs
            httpContext.Response.Redirect("/login");
            return ValueTask.FromResult(true);
        }
    }
}
