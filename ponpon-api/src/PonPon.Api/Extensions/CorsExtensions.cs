namespace PonPon.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "PonPonWeb";

    public static IServiceCollection AddPonPonCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? [];
        var webAppBaseUrl = configuration["Line:WebAppBaseUrl"];

        var normalizedOrigins = allowedOrigins
            .Append(webAppBaseUrl)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(NormalizeOrigin)
            .Where(origin => origin is not null)
            .Select(origin => origin!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                        IsAllowedOrigin(origin, normalizedOrigins))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static bool IsAllowedOrigin(
        string origin,
        IReadOnlySet<string> allowedOrigins)
    {
        var normalizedOrigin = NormalizeOrigin(origin);
        if (normalizedOrigin is null)
            return false;

        if (allowedOrigins.Contains(normalizedOrigin))
            return true;

        return Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri)
            && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOrigin(string? origin)
    {
        if (!Uri.TryCreate(origin?.Trim(), UriKind.Absolute, out var uri))
            return null;

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }
}
