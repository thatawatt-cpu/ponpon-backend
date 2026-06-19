namespace PonPon.Modules.Identity.Infrastructure.Line;

public sealed class LineOptions
{
    public string ChannelId { get; init; } = string.Empty;
    public string ChannelSecret { get; init; } = string.Empty;
}
