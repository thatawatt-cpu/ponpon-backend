namespace PonPon.Modules.Promotion.Domain;

public sealed class CouponCampaign
{
    private CouponCampaign()
    {
        Name = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static CouponCampaign Create(CouponCampaignInput input, DateTime now)
    {
        var campaign = new CouponCampaign
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = now
        };
        campaign.Update(input, now);
        return campaign;
    }

    public void Update(CouponCampaignInput input, DateTime now)
    {
        Name = input.Name.Trim();
        Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        StartsAtUtc = input.StartsAtUtc;
        EndsAtUtc = input.EndsAtUtc;
        IsActive = input.IsActive;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}

public sealed record CouponCampaignInput(
    string Name,
    string? Description,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool IsActive);
