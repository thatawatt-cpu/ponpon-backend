using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Application.Features.Uploads;

public sealed class UploadAdminFileHandler
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly ISupabaseStorageService _storage;

    public UploadAdminFileHandler(ISupabaseStorageService storage) => _storage = storage;

    public async Task<UploadAdminFileResponse> HandleAsync(UploadAdminFileCommand command, CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(command.ContentType))
            throw new BadRequestException("Only jpeg, png, webp, and gif uploads are allowed.");

        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        var path = $"home-slides/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid()}{extension}";
        var url = await _storage.UploadAsync(path, command.FileStream, command.ContentType, cancellationToken);

        return new UploadAdminFileResponse(url);
    }
}
