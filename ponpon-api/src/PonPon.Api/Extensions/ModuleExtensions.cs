using PonPon.Modules.Catalog;
using PonPon.Modules.Identity;
using PonPon.Modules.Ordering;

namespace PonPon.Api.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddPonPonModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityModule(configuration);
        services.AddCatalogModule(configuration);
        services.AddOrderingModule(configuration);
        return services;
    }
}
