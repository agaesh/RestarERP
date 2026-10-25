using Microsoft.AspNetCore.Diagnostics;
using RestarProduct.DTOs;
using RestarProduct.Exceptions;

namespace RestarProduct.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message, error) = exception switch
        {
            MultiUOMOwnerNotFoundException =>
                (StatusCodes.Status404NotFound, "The requested owner was not found.", exception.Message),
            MultiUOMAlreadyExistsException =>
                (StatusCodes.Status409Conflict, "The alternate UOM already exists for this owner.", exception.Message),
            UOMCodeAlreadyExistsException =>
                (StatusCodes.Status409Conflict, "The UOM code is already in use.", exception.Message),
            MultiUOMSetupNotFoundException =>
                (StatusCodes.Status400BadRequest, "The request contains an unknown UOM.", exception.Message),
            MultiUOMReferenceMismatchException =>
                (StatusCodes.Status400BadRequest, "The request owner does not match the route owner.", exception.Message),
            MultiUOMInvalidConfigurationException =>
                (StatusCodes.Status400BadRequest, "The MultiUOM configuration is invalid.", exception.Message),
            ArgumentException argumentException =>
                (StatusCodes.Status400BadRequest, "The request contains invalid data.", argumentException.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "Internal server error.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception during {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Handled API exception with status code {StatusCode}",
                statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiResponse(false, message, error),
            cancellationToken);

        return true;
    }
}
