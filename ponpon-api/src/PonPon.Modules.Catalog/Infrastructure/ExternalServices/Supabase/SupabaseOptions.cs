namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Supabase;

public sealed class SupabaseOptions
{
    public string Url { get; init; } = string.Empty;
    public string ServiceRoleKey { get; init; } = string.Empty;
    public string StorageBucket { get; init; } = string.Empty;
}
