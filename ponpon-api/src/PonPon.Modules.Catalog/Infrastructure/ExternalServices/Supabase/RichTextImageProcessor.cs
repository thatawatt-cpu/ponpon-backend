using System.Text.RegularExpressions;

namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;

public sealed class RichTextImageProcessor : IRichTextImageProcessor
{
    private readonly ISupabaseStorageService _storage;

    private static readonly Regex DataUriPattern = new(
        @"src=""(data:image/(?<type>[a-zA-Z]+);base64,(?<data>[A-Za-z0-9+/=]+))""",
        RegexOptions.Compiled);

    public RichTextImageProcessor(ISupabaseStorageService storage)
    {
        _storage = storage;
    }

    public async Task<string?> ProcessAsync(string? html, Guid productId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        var matches = DataUriPattern.Matches(html);
        if (matches.Count == 0)
            return html;

        foreach (Match match in matches)
        {
            var imageType = match.Groups["type"].Value.ToLowerInvariant();
            var base64Data = match.Groups["data"].Value;

            var extension = imageType switch
            {
                "jpeg" or "jpg" => ".jpg",
                "png" => ".png",
                "gif" => ".gif",
                "webp" => ".webp",
                _ => $".{imageType}"
            };

            var bytes = Convert.FromBase64String(base64Data);
            using var stream = new MemoryStream(bytes);

            var path = $"products/{productId}/rich-content/{Guid.NewGuid()}{extension}";
            var url = await _storage.UploadAsync(path, stream, $"image/{imageType}", cancellationToken);

            html = html.Replace(match.Groups[1].Value, url);
        }

        return html;
    }
}
