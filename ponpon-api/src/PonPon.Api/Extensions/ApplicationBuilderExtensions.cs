using PonPon.Api.Middlewares;

namespace PonPon.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UsePonPonMiddlewares(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
