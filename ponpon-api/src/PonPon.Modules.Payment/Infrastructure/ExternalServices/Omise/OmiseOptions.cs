namespace PonPon.Modules.Payment.Infrastructure.ExternalServices.Omise;

public sealed class OmiseOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}
