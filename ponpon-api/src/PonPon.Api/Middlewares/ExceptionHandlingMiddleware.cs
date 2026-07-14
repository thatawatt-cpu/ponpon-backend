using PonPon.Shared.Application.Exceptions;
using PonPon.Shared.Contracts;

namespace PonPon.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadRequestException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.ErrorCode, ex.Message, ex.Details);
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "unauthorized", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, ex.ErrorCode, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, "not_found", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "business_rule", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            if (!context.Response.HasStarted)
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "internal_error",
                    "An unexpected error occurred.");
            }
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        object? details = null)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(code, message, details));
    }
}
