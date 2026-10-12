using _CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Request failed with an unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            if (context.Response.HasStarted)
                throw;

            var status = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                ConflictException => StatusCodes.Status409Conflict,
                ForbiddenException => StatusCodes.Status403Forbidden,
                BusinessRuleException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
            var title = exception switch
            {
                NotFoundException => "Resource not found",
                UnauthorizedException => "Unauthorized",
                ConflictException => "Conflict",
                ForbiddenException => "Forbidden",
                BusinessRuleException => "Business rule violation",
                _ => "An unexpected error occurred"
            };
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception is DomainException || environment.IsDevelopment() ? exception.Message : null,
                Instance = context.Request.Path.Value
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (exception is DomainException domainException)
                problem.Extensions["code"] = domainException.Code;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }


}
