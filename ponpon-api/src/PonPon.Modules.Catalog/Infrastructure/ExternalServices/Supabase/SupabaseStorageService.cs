using Microsoft.Extensions.Options;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;

public sealed class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly IRuntimeSettingProvider _settings;

    public SupabaseStorageService(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        IRuntimeSettingProvider settings)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _settings = settings;
    }

    public async Task<string> UploadAsync(string path, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        var uploadUrl = $"{options.Url.TrimEnd('/')}/storage/v1/object/{options.StorageBucket}/{path}";
        var publicUrl = $"{options.Url.TrimEnd('/')}/storage/v1/object/public/{options.StorageBucket}/{path}";

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Headers.Add("Authorization", $"Bearer {options.ServiceRoleKey}");
        request.Headers.Add("x-upsert", "true");
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return publicUrl;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var options = await ResolveOptionsAsync(cancellationToken);
        EnsureConfigured(options);
        var deleteUrl = $"{options.Url.TrimEnd('/')}/storage/v1/object/{options.StorageBucket}/{path}";
        using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
        request.Headers.Add("Authorization", $"Bearer {options.ServiceRoleKey}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<ResolvedSupabaseOptions> ResolveOptionsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetGroupAsync("Supabase", cancellationToken);
        return new ResolvedSupabaseOptions(
            Get(settings, "Url", _options.Url),
            Get(settings, "ServiceRoleKey", _options.ServiceRoleKey),
            Get(settings, "StorageBucket", _options.StorageBucket));
    }

    private static void EnsureConfigured(ResolvedSupabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Url)
            || string.IsNullOrWhiteSpace(options.ServiceRoleKey)
            || string.IsNullOrWhiteSpace(options.StorageBucket))
        {
            throw new BadRequestException("Supabase storage settings are not configured.");
        }
    }

    private static string Get(IReadOnlyDictionary<string, string?> settings, string key, string fallback)
        => settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private sealed record ResolvedSupabaseOptions(string Url, string ServiceRoleKey, string StorageBucket);
}
