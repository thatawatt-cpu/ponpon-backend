using PonPon.Api.Extensions;
using PonPon.Api.Realtime;
using PonPon.Modules.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddPonPonServices(builder.Configuration);
builder.Services.AddPonPonModules(builder.Configuration);

var app = builder.Build();

await app.Services.SeedIdentityModuleAsync();

app.UsePonPonMiddlewares();
app.MapControllers();
app.MapHub<ShopNotificationHub>("/hubs/shop-notifications");

app.Run();

public partial class Program;
