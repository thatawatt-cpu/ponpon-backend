namespace PonPon.Modules.Promotion.Domain;

public sealed class Coupon
{
    private Coupon() { Code = string.Empty; Name = string.Empty; Type = string.Empty; }

    public Guid Id { get; private set; }
    public Guid? CampaignId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinimumSubtotal { get; private set; }
    public decimal? MaximumDiscount { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public bool CanCombineWithFlashSale { get; private set; }
    public bool CanStackWithPromotions { get; private set; }
    public bool CanStackWithCoupons { get; private set; }
    public int? MaximumTotalUses { get; private set; }
    public int? MaximumUsesPerCustomer { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public List<CouponScope> Scopes { get; private set; } = [];
    public List<CouponCustomerScope> CustomerScopes { get; private set; } = [];
    public List<CouponCondition> Conditions { get; private set; } = [];

    public static Coupon Create(CouponInput input, DateTime now)
    {
        var coupon = new Coupon { Id = Guid.NewGuid(), CreatedAtUtc = now };
        coupon.Update(input, now);
        return coupon;
    }

    public void Update(CouponInput input, DateTime now)
    {
        Code = input.Code.Trim().ToUpperInvariant();
        Name = string.IsNullOrWhiteSpace(input.Name) ? Code : input.Name.Trim();
        Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        CampaignId = input.CampaignId;
        Type = input.Type.Trim().ToLowerInvariant();
        Value = input.Value;
        MinimumSubtotal = input.MinimumSubtotal;
        MaximumDiscount = input.MaximumDiscount;
        StartsAtUtc = input.StartsAtUtc;
        EndsAtUtc = input.EndsAtUtc;
        CanCombineWithFlashSale = input.CanCombineWithFlashSale;
        CanStackWithPromotions = input.CanStackWithPromotions;
        CanStackWithCoupons = input.CanStackWithCoupons;
        MaximumTotalUses = input.MaximumTotalUses;
        MaximumUsesPerCustomer = input.MaximumUsesPerCustomer;
        IsActive = input.IsActive;
        UpdatedAtUtc = now;
        Scopes.Clear();
        Scopes.AddRange(input.Scopes.Select(CouponScope.Create));
        CustomerScopes.Clear();
        CustomerScopes.AddRange(input.CustomerScopes.Select(CouponCustomerScope.Create));
        Conditions.Clear();
        Conditions.AddRange(input.Conditions.Select(CouponCondition.Create));
    }

    public void IncrementUsage() => UsedCount++;
    public void DecrementUsage() => UsedCount = Math.Max(0, UsedCount - 1);
    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    public void Delete(DateTime now)
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = now;
    }
}

public sealed record CouponInput(
    string Code,
    string Type,
    decimal Value,
    decimal MinimumSubtotal,
    decimal? MaximumDiscount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool CanCombineWithFlashSale,
    int? MaximumTotalUses,
    int? MaximumUsesPerCustomer,
    bool IsActive,
    IReadOnlyCollection<CouponScopeInput>? Scopes = null,
    IReadOnlyCollection<CouponCustomerScopeInput>? CustomerScopes = null,
    IReadOnlyCollection<CouponConditionInput>? Conditions = null,
    bool CanStackWithPromotions = true,
    bool CanStackWithCoupons = true,
    Guid? CampaignId = null,
    string? Name = null,
    string? Description = null)
{
    public IReadOnlyCollection<CouponScopeInput> Scopes { get; } = Scopes ?? [];
    public IReadOnlyCollection<CouponCustomerScopeInput> CustomerScopes { get; } = CustomerScopes ?? [];
    public IReadOnlyCollection<CouponConditionInput> Conditions { get; } = Conditions ?? [];
}

public sealed class CouponScope
{
    private CouponScope()
    {
        Type = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid CouponId { get; private set; }
    public string Type { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string? Sku { get; private set; }
    public long? ZortCategoryId { get; private set; }
    public string? CategoryName { get; private set; }

    public static CouponScope Create(CouponScopeInput input)
    {
        return new CouponScope
        {
            Id = Guid.NewGuid(),
            Type = input.Type.Trim().ToLowerInvariant(),
            ProductId = input.ProductId,
            VariantId = input.VariantId,
            Sku = string.IsNullOrWhiteSpace(input.Sku) ? null : input.Sku.Trim().ToUpperInvariant(),
            ZortCategoryId = input.ZortCategoryId,
            CategoryName = string.IsNullOrWhiteSpace(input.CategoryName) ? null : input.CategoryName.Trim()
        };
    }
}

public sealed record CouponScopeInput(
    string Type,
    Guid? ProductId = null,
    Guid? VariantId = null,
    string? Sku = null,
    long? ZortCategoryId = null,
    string? CategoryName = null);

public sealed class CouponCustomerScope
{
    private CouponCustomerScope()
    {
        Type = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid CouponId { get; private set; }
    public string Type { get; private set; }
    public Guid? CustomerId { get; private set; }

    public static CouponCustomerScope Create(CouponCustomerScopeInput input)
    {
        return new CouponCustomerScope
        {
            Id = Guid.NewGuid(),
            Type = input.Type.Trim().ToLowerInvariant(),
            CustomerId = input.CustomerId
        };
    }
}

public sealed record CouponCustomerScopeInput(
    string Type,
    Guid? CustomerId = null);

public sealed class CouponCondition
{
    private CouponCondition()
    {
        Type = string.Empty;
        Value = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid CouponId { get; private set; }
    public string Type { get; private set; }
    public string Value { get; private set; }

    public static CouponCondition Create(CouponConditionInput input) => new()
    {
        Id = Guid.NewGuid(),
        Type = input.Type.Trim().ToLowerInvariant(),
        Value = input.Value.Trim().ToLowerInvariant()
    };
}

public sealed record CouponConditionInput(string Type, string Value);
