namespace PonPon.Modules.Shipping.Infrastructure.ExternalServices.Shippop;

public sealed class ShippopOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://mkpservice.shippop.dev/";
}
