namespace PonPon.Shared.Application.Abstractions;

public interface IZortWebhookRegistrar
{
    Task RegisterAsync(string baseUrl, string key1, string? key2 = null, string? key3 = null, CancellationToken cancellationToken = default);
    Task<ZortWebhookInfo> GetAsync(CancellationToken cancellationToken = default);
}
