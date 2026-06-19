using PonPon.Shared.Application.Exceptions;
using PonPon.Shared.Contracts;

namespace PonPon.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadRequestException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "bad_request", ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "unauthorized", ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, "not_found", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "business_rule", ex.Message);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(code, message));
    }
}
