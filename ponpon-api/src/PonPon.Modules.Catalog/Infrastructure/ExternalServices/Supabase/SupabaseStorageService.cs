using Microsoft.Extensions.Options;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;

public sealed class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;

    public SupabaseStorageService(HttpClient httpClient, IOptions<SupabaseOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(string path, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        var uploadUrl = $"storage/v1/object/{_options.StorageBucket}/{path}";
        var publicUrl = $"{_options.Url.TrimEnd('/')}/storage/v1/object/public/{_options.StorageBucket}/{path}";

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Headers.Add("Authorization", $"Bearer {_options.ServiceRoleKey}");
        request.Headers.Add("x-upsert", "true");
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return publicUrl;
    }
}
