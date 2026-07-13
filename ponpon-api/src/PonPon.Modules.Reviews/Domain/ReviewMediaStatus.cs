namespace PonPon.Modules.Reviews.Domain;

public static class ReviewMediaStatus
{
    public const string Processing = "processing";
    public const string Ready = "ready";
    public const string Failed = "failed";

    public static bool IsValid(string status)
    {
        return string.Equals(status, Processing, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, Ready, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, Failed, StringComparison.OrdinalIgnoreCase);
    }
}
