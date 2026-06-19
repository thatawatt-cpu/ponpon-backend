using PonPon.Shared.Domain;

namespace PonPon.Modules.Identity.Domain.Customers;

public sealed class LineProfile : ValueObject
{
    private LineProfile()
    {
        LineUserId = string.Empty;
        DisplayName = string.Empty;
    }

    public LineProfile(string lineUserId, string displayName, string? pictureUrl, string? email)
    {
        LineUserId = lineUserId;
        DisplayName = displayName;
        PictureUrl = pictureUrl;
        Email = email;
    }

    public string LineUserId { get; private set; }
    public string DisplayName { get; private set; }
    public string? PictureUrl { get; private set; }
    public string? Email { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LineUserId;
        yield return DisplayName;
        yield return PictureUrl;
        yield return Email;
    }
}
