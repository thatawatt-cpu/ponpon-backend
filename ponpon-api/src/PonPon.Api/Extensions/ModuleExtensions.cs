using PonPon.Modules.Catalog;
using PonPon.Modules.Identity;
using PonPon.Modules.Notification;
using PonPon.Modules.Ordering;
using PonPon.Modules.Payment;
using PonPon.Modules.Settings;
using PonPon.Modules.Shipping;
using PonPon.Modules.Promotion;

namespace PonPon.Api.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddPonPonModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityModule(configuration);
        services.AddCatalogModule(configuration);
        services.AddPromotionModule(configuration);
        services.AddOrderingModule(configuration);
        services.AddShippingModule(configuration);
        services.AddPaymentModule(configuration);
        services.AddSettingsModule(configuration);
        services.AddNotificationModule(configuration);
        return services;
    }
}
