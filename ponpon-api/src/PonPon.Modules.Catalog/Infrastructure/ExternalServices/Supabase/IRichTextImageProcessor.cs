namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;

public interface IRichTextImageProcessor
{
    Task<string?> ProcessAsync(string? html, Guid productId, CancellationToken cancellationToken = default);
}
