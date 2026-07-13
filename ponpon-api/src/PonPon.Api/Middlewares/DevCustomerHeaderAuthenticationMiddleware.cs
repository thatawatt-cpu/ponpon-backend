using System.Security.Claims;
using Microsoft.Extensions.Hosting;

namespace PonPon.Api.Middlewares;

public sealed class DevCustomerHeaderAuthenticationMiddleware
{
    public const string HeaderName = "X-Dev-Customer-Id";
    private static readonly Guid DevCustomerId = new("77e02c7d-9578-47e8-bd07-e4e336e1e1c9");

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public DevCustomerHeaderAuthenticationMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_env.IsDevelopment()
            || context.User.Identity?.IsAuthenticated == true
            || !context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            await _next(context);
            return;
        }

        var rawCustomerId = values.FirstOrDefault();
        if (Guid.TryParse(rawCustomerId, out var customerId) && customerId == DevCustomerId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, DevCustomerId.ToString("D")),
                new Claim("sub", DevCustomerId.ToString("D")),
                new Claim("userType", "Customer"),
                new Claim("customerId", DevCustomerId.ToString("D")),
                new Claim("lineUserId", $"dev:{DevCustomerId:D}")
            };

            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: "DevCustomerHeader"));
        }

        await _next(context);
    }
}
