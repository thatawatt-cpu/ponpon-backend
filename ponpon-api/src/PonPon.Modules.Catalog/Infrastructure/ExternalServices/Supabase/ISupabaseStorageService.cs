namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;

public interface ISupabaseStorageService
{
    Task<string> UploadAsync(string path, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
}
