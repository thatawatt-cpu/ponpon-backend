using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PonPon.Modules.Identity.Application.Abstractions;
using PonPon.Modules.Identity.Application.Features.AdminLogin;
using PonPon.Modules.Identity.Application.Features.GetMe;
using PonPon.Modules.Identity.Application.Features.LineLogin;
using PonPon.Modules.Identity.Application.Features.Logout;
using PonPon.Modules.Identity.Application.Features.RefreshToken;
using PonPon.Modules.Identity.Infrastructure.Jwt;
using PonPon.Modules.Identity.Infrastructure.Line;
using PonPon.Modules.Identity.Infrastructure.Persistence;
using PonPon.Modules.Identity.Infrastructure.Persistence.Repositories;
using PonPon.Modules.Identity.Infrastructure.Security;
using PonPon.Modules.Identity.Infrastructure.Seeding;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Infrastructure.Time;

namespace PonPon.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<LineOptions>(configuration.GetSection("Line"));
        services.Configure<SeedAdminOptions>(configuration.GetSection("SeedAdmin"));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema)));

        services.AddScoped<IUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<HttpClient>();
        services.AddScoped<ILineAuthService, LineAuthService>();

        services.AddScoped<LineLoginHandler>();
        services.AddScoped<AdminLoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<GetMeHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<IdentityDataSeeder>();

        return services;
    }
}
