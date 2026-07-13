namespace PonPon.Modules.Settings.Application.Features.IntegrationSettings;

public sealed record IntegrationSettingsResponse(
    IReadOnlyCollection<IntegrationSettingsGroupResponse> Groups);

public sealed record IntegrationSettingsGroupResponse(
    string Group,
    string DisplayName,
    IReadOnlyCollection<IntegrationSettingFieldResponse> Fields);

public sealed record IntegrationSettingFieldResponse(
    string Key,
    string Label,
    bool IsSecret,
    bool IsConfigured,
    string? Value);

public sealed record UpdateIntegrationSettingsRequest(
    IReadOnlyDictionary<string, string?> Values);
