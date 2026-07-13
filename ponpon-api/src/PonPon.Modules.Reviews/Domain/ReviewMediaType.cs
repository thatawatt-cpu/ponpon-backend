namespace PonPon.Modules.Reviews.Domain;

public static class ReviewMediaType
{
    public const string Image = "image";
    public const string Video = "video";

    public static bool IsValid(string type)
    {
        return string.Equals(type, Image, StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, Video, StringComparison.OrdinalIgnoreCase);
    }
}
