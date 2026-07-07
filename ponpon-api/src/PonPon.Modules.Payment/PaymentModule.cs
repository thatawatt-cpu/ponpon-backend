using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PonPon.Modules.Payment.Application.Abstractions;
using PonPon.Modules.Payment.Application.Features.CreateCreditCardCharge;
using PonPon.Modules.Payment.Application.Features.CreateMobileBankingCharge;
using PonPon.Modules.Payment.Application.Features.CreatePromptPayCharge;
using PonPon.Modules.Payment.Application.Features.GetChargeStatus;
using PonPon.Modules.Payment.Application.Features.HandleOmiseWebhook;
using PonPon.Modules.Payment.Infrastructure.ExternalServices.Omise;
using PonPon.Modules.Payment.Application.Services;
using PonPon.Modules.Payment.Application;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Payment;

public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OmiseOptions>(configuration.GetSection("Omise"));

        services.AddHttpClient<IOmiseClient, OmiseClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.omise.co/");
        });

        services.AddScoped<CreatePromptPayChargeHandler>();
        services.AddScoped<CreateMobileBankingChargeHandler>();
        services.AddScoped<CreateCreditCardChargeHandler>();
        services.AddScoped<GetChargeStatusHandler>();
        services.AddScoped<HandleOmiseWebhookHandler>();
        services.AddScoped<IOrderPaymentRefundService, OmiseOrderPaymentRefundService>();
        services.AddScoped<PaymentChargeCoordinator>();

        return services;
    }
}
