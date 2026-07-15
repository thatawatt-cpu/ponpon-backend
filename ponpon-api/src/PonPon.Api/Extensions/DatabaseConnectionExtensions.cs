using System.Data.Common;

namespace PonPon.Api.Extensions;

public static class DatabaseConnectionExtensions
{
    public static void UseConstrainedDatabaseConnectionPool(this ConfigurationManager configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        SetDefault(builder, "Maximum Pool Size", GetPositiveInt(configuration["Database:MaximumPoolSize"]) ?? 10);
        SetDefault(builder, "Minimum Pool Size", GetPositiveInt(configuration["Database:MinimumPoolSize"]) ?? 0);
        SetDefault(builder, "Connection Idle Lifetime", GetPositiveInt(configuration["Database:ConnectionIdleLifetimeSeconds"]) ?? 30);
        SetDefault(builder, "Connection Pruning Interval", GetPositiveInt(configuration["Database:ConnectionPruningIntervalSeconds"]) ?? 10);

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = builder.ConnectionString
        });
    }

    public static int GetHangfireWorkerCount(this IConfiguration configuration)
        => GetPositiveInt(configuration["Hangfire:WorkerCount"]) ?? 2;

    private static void SetDefault(DbConnectionStringBuilder builder, string key, int value)
    {
        if (!builder.ContainsKey(key))
        {
            builder[key] = value;
        }
    }

    private static int? GetPositiveInt(string? value)
        => int.TryParse(value, out var result) && result > 0 ? result : null;
}
