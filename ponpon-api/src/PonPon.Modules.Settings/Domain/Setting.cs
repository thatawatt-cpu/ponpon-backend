namespace PonPon.Modules.Settings.Domain;

public sealed class Setting
{
    private Setting() { }

    public Guid Id { get; private set; }
    public string Group { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Value { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Setting Create(string group, string key, string? value, DateTime now)
    {
        return new Setting
        {
            Id = Guid.NewGuid(),
            Group = group,
            Key = key,
            Value = value,
            UpdatedAtUtc = now
        };
    }

    public void Update(string? value, DateTime now)
    {
        Value = value;
        UpdatedAtUtc = now;
    }
}
