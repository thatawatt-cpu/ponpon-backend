namespace PonPon.Modules.Settings.Application.Features.UpsertSetting;

public sealed record UpsertSettingCommand(string Group, string Key, string? Value);
