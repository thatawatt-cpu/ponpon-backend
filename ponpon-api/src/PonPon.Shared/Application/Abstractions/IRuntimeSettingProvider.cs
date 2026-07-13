namespace PonPon.Shared.Application.Abstractions;

public interface IRuntimeSettingProvider
{
    Task<string?> GetValueAsync(string group, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(string group, CancellationToken cancellationToken = default);
}
