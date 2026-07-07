namespace PonPon.Modules.Settings.Application.Features.GetSettings;

public sealed record SettingResponse(string Group, string Key, string? Value);
