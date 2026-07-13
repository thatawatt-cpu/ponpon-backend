namespace PonPon.Modules.Reviews.Domain;

public static class ReviewStatus
{
    public const string Published = "published";
    public const string Hidden = "hidden";

    public static bool IsValid(string status)
    {
        return string.Equals(status, Published, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, Hidden, StringComparison.OrdinalIgnoreCase);
    }
}
