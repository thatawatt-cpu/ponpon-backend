using Microsoft.AspNetCore.Mvc;
using PonPon.Api.Extensions;
using PonPon.Api.Realtime;
using PonPon.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Configuration.UseConstrainedDatabaseConnectionPool();

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid request payload." : x.ErrorMessage)
            .FirstOrDefault() ?? "Invalid request payload.";

        return new BadRequestObjectResult(new ErrorResponse("validation_error", message));
    };
});
builder.Services.AddPonPonServices(builder.Configuration);
builder.Services.AddPonPonModules(builder.Configuration);

var app = builder.Build();

app.Map("/api/health", health => health.Run(context => context.Response.WriteAsJsonAsync(new
{
    status = "ok",
    timestampUtc = DateTime.UtcNow
})));
app.UsePonPonMiddlewares();
app.MapControllers();
app.MapHub<ShopNotificationHub>("/hubs/shop-notifications");

app.Run();

public partial class Program;
