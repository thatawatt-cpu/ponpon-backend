using PonPon.Api.Services;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Infrastructure.Messaging;

namespace PonPon.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPonPonServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddPonPonAuthentication(configuration);
        return services;
    }
}
