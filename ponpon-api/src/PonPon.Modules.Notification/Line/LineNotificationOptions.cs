namespace PonPon.Modules.Notification.Line;

public sealed class LineNotificationOptions
{
    public string ChannelAccessToken { get; set; } = string.Empty;
    public string[] AdminRecipientIds { get; set; } = [];
    public string WebAppBaseUrl { get; set; } = string.Empty;
}
